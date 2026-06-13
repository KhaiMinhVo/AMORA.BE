using Amora.Application.Abstractions;
using Amora.Application.Dtos.Messages;
using Amora.Application.Exceptions;
using Amora.Application.Features.Pets.Commands;
using Amora.Application.Pets;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;
using MediatR;

namespace Amora.Application.Services;

public sealed class ChatService
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IMatchConnectionRepository _matchConnectionRepository;
    private readonly IChatMessageRepository _chatMessageRepository;
    private readonly IRealtimeNotifier _realtimeNotifier;
    private readonly IMediator _mediator;
    private readonly PetFeatureGateService _featureGate;
    private readonly IChatReadStateRepository _readState;
    private readonly AiModerationService _aiModerationService;

    public ChatService(
        ICurrentUserService currentUserService,
        IMatchConnectionRepository matchConnectionRepository,
        IChatMessageRepository chatMessageRepository,
        IRealtimeNotifier realtimeNotifier,
        IMediator mediator,
        PetFeatureGateService featureGate,
        IChatReadStateRepository readState,
        AiModerationService aiModerationService)
    {
        _currentUserService = currentUserService;
        _matchConnectionRepository = matchConnectionRepository;
        _chatMessageRepository = chatMessageRepository;
        _realtimeNotifier = realtimeNotifier;
        _mediator = mediator;
        _featureGate = featureGate;
        _readState = readState;
        _aiModerationService = aiModerationService;
    }

    public async Task<MessageHistoryResponseDto> GetHistoryAsync(Guid matchId, string? cursor, int limit, CancellationToken cancellationToken = default)
    {
        if (!await _matchConnectionRepository.IsParticipantAsync(matchId, _currentUserService.UserId, cancellationToken))
        {
            throw new ForbiddenApiException("You cannot access this chat room.");
        }

        limit = Math.Clamp(limit, 1, 50);
        var (items, nextCursor) = await _chatMessageRepository.GetByMatchAsync(matchId, cursor, limit, cancellationToken);

        if (items.Count > 0)
        {
            await _readState.UpsertReadAsync(
                _currentUserService.UserId,
                matchId,
                items.Max(x => x.CreatedAt),
                cancellationToken);
        }

        return new MessageHistoryResponseDto
        {
            NextCursor = nextCursor,
            Items = items.Select(message => new MessageItemDto
            {
                MessageId = message.Id,
                SenderId = message.SenderId,
                Type = message.MessageType.ToString(),
                ContentUrl = message.ContentUrl,
                Content = message.Content,
                Duration = message.Duration,
                CreatedAt = message.CreatedAt
            }).ToList()
        };
    }

    public async Task<SendMessageResponseDto> SendMessageAsync(Guid matchId, SendMessageRequest request, CancellationToken cancellationToken = default)
    {
        if (!await _matchConnectionRepository.IsParticipantAsync(matchId, _currentUserService.UserId, cancellationToken))
        {
            throw new ForbiddenApiException("You cannot send messages to this room.");
        }

        // Handshake 24h: chặn gửi tin vào match đã hết hạn
        var match = await _matchConnectionRepository.GetByIdAsync(matchId, cancellationToken);
        if (match is null || match.Status != Domain.Enums.MatchStatus.Active)
        {
            throw new ValidationApiException("Match đã hết hạn hoặc không còn hoạt động.");
        }

        if (!Enum.TryParse<MessageType>(request.Type, ignoreCase: true, out var messageType))
        {
            throw new ValidationApiException("Unsupported message type.");
        }

        if (messageType is MessageType.Voice or MessageType.Image && string.IsNullOrWhiteSpace(request.ContentUrl))
        {
            throw new ValidationApiException("ContentUrl is required for voice/image messages.");
        }

        await _featureGate.ValidateSendAsync(matchId, messageType, cancellationToken);
        if (messageType == MessageType.Image)
            await _featureGate.RegisterImageSentAsync(matchId, _currentUserService.UserId, cancellationToken);

        if (messageType == MessageType.Text)
        {
            throw new ValidationApiException("Text messages are not allowed. Please use Voice messages.");
        }

        if (messageType == MessageType.System)
        {
            throw new ValidationApiException("You cannot send System messages.");
        }

        var message = new ChatMessage
        {
            Id = Guid.NewGuid().ToString("N")[..24], // 24 hex chars for MongoDB ObjectId
            MatchId = matchId,
            SenderId = _currentUserService.UserId,
            MessageType = messageType,
            ContentUrl = request.ContentUrl,
            Content = request.Content,
            Duration = request.Duration,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _chatMessageRepository.AddAsync(message, cancellationToken);

        // Pet System — chỉ metadata / loại tin, không đọc nội dung
        if (messageType == MessageType.Voice)
        {
            await _mediator.Send(new PublishVoiceForVibeCommand(
                matchId,
                _currentUserService.UserId,
                request.ContentUrl!,
                request.Duration ?? 0), cancellationToken);
        }
        else if (messageType == MessageType.Image)
        {
            await _mediator.Send(new ProcessTextMessagePetCommand(matchId, _currentUserService.UserId), cancellationToken);
        }

        // Handshake 24h: Gỡ bỏ thời hạn khi có tin nhắn đầu tiên, giúp Match trở thành vĩnh viễn.
        await _matchConnectionRepository.CompleteHandshakeAsync(matchId, cancellationToken);
        DateTimeOffset? expiresAt = null;
        
        var partnerId = match.UserAId == _currentUserService.UserId ? match.UserBId : match.UserAId;
        var partnerState = await _readState.GetAsync(partnerId, matchId, cancellationToken);
        var since = partnerState?.LastReadAt ?? DateTimeOffset.MinValue;
        var unreadCount = await _readState.CountUnreadAsync(partnerId, matchId, since, cancellationToken);

        await _realtimeNotifier.NotifyNewMessageAsync(message, unreadCount, expiresAt, cancellationToken);

        return new SendMessageResponseDto
        {
            MessageId = message.Id,
            Status = "Sent",
            CreatedAt = message.CreatedAt
        };
    }

    public async Task<MarkMessagesAsReadResponseDto> MarkMessagesAsReadAsync(Guid matchId, MarkMessagesAsReadRequest request, CancellationToken cancellationToken = default)
    {
        if (!await _matchConnectionRepository.IsParticipantAsync(matchId, _currentUserService.UserId, cancellationToken))
        {
            throw new ForbiddenApiException("You cannot access this chat room.");
        }

        var message = await _chatMessageRepository.GetByIdAsync(request.MessageId, cancellationToken);
        if (message is null || message.MatchId != matchId)
        {
            throw new NotFoundApiException("Message not found in this match.");
        }

        await _readState.UpsertReadAsync(_currentUserService.UserId, matchId, message.CreatedAt, cancellationToken);
        var unreadCount = await _readState.CountUnreadAsync(_currentUserService.UserId, matchId, message.CreatedAt, cancellationToken);

        await _realtimeNotifier.NotifyMessagesReadAsync(matchId, _currentUserService.UserId, request.MessageId, unreadCount, cancellationToken);

        return new MarkMessagesAsReadResponseDto
        {
            MatchId = matchId,
            LastReadMessageId = request.MessageId,
            UnreadCount = unreadCount
        };
    }
}