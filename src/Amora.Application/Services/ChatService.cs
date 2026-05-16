using Amora.Application.Abstractions;
using Amora.Application.Dtos.Messages;
using Amora.Application.Exceptions;
using Amora.Application.Features.Pets.Commands;
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

    public ChatService(
        ICurrentUserService currentUserService,
        IMatchConnectionRepository matchConnectionRepository,
        IChatMessageRepository chatMessageRepository,
        IRealtimeNotifier realtimeNotifier,
        IMediator mediator)
    {
        _currentUserService = currentUserService;
        _matchConnectionRepository = matchConnectionRepository;
        _chatMessageRepository = chatMessageRepository;
        _realtimeNotifier = realtimeNotifier;
        _mediator = mediator;
    }

    public async Task<MessageHistoryResponseDto> GetHistoryAsync(Guid matchId, string? cursor, int limit, CancellationToken cancellationToken = default)
    {
        if (!await _matchConnectionRepository.IsParticipantAsync(matchId, _currentUserService.UserId, cancellationToken))
        {
            throw new ForbiddenApiException("You cannot access this chat room.");
        }

        limit = Math.Clamp(limit, 1, 50);
        var (items, nextCursor) = await _chatMessageRepository.GetByMatchAsync(matchId, cursor, limit, cancellationToken);

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

        if (messageType is MessageType.Voice && string.IsNullOrWhiteSpace(request.ContentUrl))
        {
            throw new ValidationApiException("ContentUrl is required for voice messages.");
        }

        var message = new ChatMessage
        {
            Id = Guid.NewGuid().ToString("N"),
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
        if (messageType == MessageType.Text)
        {
            await _mediator.Send(new ProcessTextMessagePetCommand(matchId, _currentUserService.UserId), cancellationToken);
        }
        else if (messageType == MessageType.Voice)
        {
            await _mediator.Send(new PublishVoiceForVibeCommand(
                matchId,
                _currentUserService.UserId,
                request.ContentUrl!,
                request.Duration ?? 0), cancellationToken);
        }

        // Handshake 24h: gia hạn thêm 24h mỗi khi có tin nhắn (trước khi push realtime)
        await _matchConnectionRepository.ExtendHandshakeAsync(matchId, cancellationToken);
        var expiresAt = DateTimeOffset.UtcNow.AddHours(24);
        await _realtimeNotifier.NotifyNewMessageAsync(message, expiresAt, cancellationToken);

        return new SendMessageResponseDto
        {
            MessageId = message.Id,
            Status = "Sent",
            CreatedAt = message.CreatedAt
        };
    }
}