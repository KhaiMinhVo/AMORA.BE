using Amora.Application.Abstractions;
using Amora.Application.Dtos.Messages;
using Amora.Application.Exceptions;
using Amora.Application.Features.Pets.Commands;
using Amora.Application.Pets;
using Amora.Application.Pets.Commands;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

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
    private readonly Microsoft.Extensions.DependencyInjection.IServiceScopeFactory _scopeFactory;
    private readonly IUserBlockRepository _userBlockRepository;
    private readonly IExpoPushService _expoPushService;

    public ChatService(
        ICurrentUserService currentUserService,
        IMatchConnectionRepository matchConnectionRepository,
        IChatMessageRepository chatMessageRepository,
        IRealtimeNotifier realtimeNotifier,
        IMediator mediator,
        PetFeatureGateService featureGate,
        IChatReadStateRepository readState,
        AiModerationService aiModerationService,
        Microsoft.Extensions.DependencyInjection.IServiceScopeFactory scopeFactory,
        IUserBlockRepository userBlockRepository,
        IExpoPushService expoPushService)
    {
        _currentUserService = currentUserService;
        _matchConnectionRepository = matchConnectionRepository;
        _chatMessageRepository = chatMessageRepository;
        _realtimeNotifier = realtimeNotifier;
        _mediator = mediator;
        _featureGate = featureGate;
        _readState = readState;
        _aiModerationService = aiModerationService;
        _scopeFactory = scopeFactory;
        _userBlockRepository = userBlockRepository;
        _expoPushService = expoPushService;
    }

    public async Task<MessageHistoryResponseDto> GetHistoryAsync(Guid matchId, string? cursor, int limit, CancellationToken cancellationToken = default)
    {
        if (!await _matchConnectionRepository.IsParticipantAsync(matchId, _currentUserService.UserId, cancellationToken))
        {
            throw new ForbiddenApiException("Bạn không thể truy cập phòng chat này.");
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
            throw new ForbiddenApiException("Bạn không thể gửi tin nhắn vào phòng chat này.");
        }

        // Handshake 24h: chặn gửi tin vào match đã hết hạn
        var match = await _matchConnectionRepository.GetByIdAsync(matchId, cancellationToken);
        if (match is null || match.Status != Domain.Enums.MatchStatus.Active)
        {
            throw new ValidationApiException("Match đã hết hạn hoặc không còn hoạt động.");
        }

        var partnerId = match.UserAId == _currentUserService.UserId ? match.UserBId : match.UserAId;
        bool isBlockedByMe = await _userBlockRepository.IsBlockedAsync(_currentUserService.UserId, partnerId, cancellationToken);
        bool isBlockedByThem = await _userBlockRepository.IsBlockedAsync(partnerId, _currentUserService.UserId, cancellationToken);
        
        if (isBlockedByMe || isBlockedByThem)
        {
            throw new ForbiddenApiException("Chat blocked", "CHAT_BLOCKED");
        }

        if (!Enum.TryParse<MessageType>(request.Type, ignoreCase: true, out var messageType))
        {
            throw new ValidationApiException("Loại tin nhắn không được hỗ trợ.");
        }

        if (messageType is MessageType.Voice or MessageType.Image && string.IsNullOrWhiteSpace(request.ContentUrl))
        {
            throw new ValidationApiException("Yêu cầu phải có link nội dung đối với tin nhắn thoại/ảnh.");
        }

        if (messageType == MessageType.Sticker && string.IsNullOrWhiteSpace(request.Content) && string.IsNullOrWhiteSpace(request.ContentUrl))
        {
            throw new ValidationApiException("Yêu cầu phải có nội dung hoặc link đối với nhãn dán.");
        }

        await _featureGate.ValidateSendAsync(matchId, messageType, cancellationToken);
        if (messageType == MessageType.Image)
            await _featureGate.RegisterImageSentAsync(matchId, _currentUserService.UserId, cancellationToken);

        if (messageType == MessageType.Text)
        {
            throw new ValidationApiException("Không hỗ trợ tin nhắn văn bản. Vui lòng gửi tin nhắn thoại.");
        }

        if (messageType == MessageType.System)
        {
            throw new ValidationApiException("Bạn không thể gửi tin nhắn hệ thống.");
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
            await _mediator.Send(new ProcessVoiceMessagePetCommand(matchId, _currentUserService.UserId, request.Duration ?? 0), cancellationToken);
            
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

        // Trigger AI Emotion Analysis for Voice and Image (if we ever support text, it will be caught here)
        if (messageType != MessageType.Sticker)
        {
            _ = Task.Run(async () => 
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var scopedMediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                    await scopedMediator.Send(new AnalyzeConversationEmotionCommand(matchId));
                }
                catch (Exception)
                {
                    // Mute background exceptions
                }
            });
        }

        // Handshake 24h: Gỡ bỏ thời hạn khi có tin nhắn đầu tiên, giúp Match trở thành vĩnh viễn.
        await _matchConnectionRepository.CompleteHandshakeAsync(matchId, cancellationToken);
        var expiresAt = DateTimeOffset.MaxValue;
        
        var partnerState = await _readState.GetAsync(partnerId, matchId, cancellationToken);
        var since = partnerState?.LastReadAt ?? DateTimeOffset.MinValue;
        var unreadCount = await _readState.CountUnreadAsync(partnerId, matchId, since, cancellationToken);

        await _realtimeNotifier.NotifyNewMessageAsync(message, unreadCount, expiresAt, cancellationToken);

        // Expo Push — gửi cho đối phương khi có tin nhắn mới
        var pushTitle = messageType switch
        {
            MessageType.Voice => "Tin nhắn thoại mới",
            MessageType.Sticker => "Nhãn dán mới",
            _ => "Tin nhắn mới"
        };
        var pushBody = messageType switch
        {
            MessageType.Voice => "Bạn nhận được một tin nhắn thoại mới.",
            MessageType.Sticker => "Bạn nhận được một nhãn dán mới.",
            _ => "Bạn nhận được một tin nhắn mới."
        };
        var pushData = new { matchId = matchId.ToString(), type = "chat" };
        _ = _expoPushService.SendPushAsync(partnerId, pushTitle, pushBody, pushData, cancellationToken);

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
            throw new ForbiddenApiException("Bạn không thể truy cập phòng chat này.");
        }

        var message = await _chatMessageRepository.GetByIdAsync(request.MessageId, cancellationToken);
        if (message is null || message.MatchId != matchId)
        {
            throw new NotFoundApiException("Không tìm thấy tin nhắn trong cuộc trò chuyện này.");
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