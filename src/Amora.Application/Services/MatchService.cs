using Amora.Application.Abstractions;
using Amora.Application.Dtos.Matches;
using Amora.Application.Exceptions;
using Amora.Application.Features.Pets.Commands;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Amora.Application.Services;

public sealed class MatchService
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IVoicePostRepository _voicePostRepository;
    private readonly IVoiceCommentRepository _voiceCommentRepository;
    private readonly IMatchConnectionRepository _matchConnectionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IChatMessageRepository _chatMessageRepository;
    private readonly IRealtimeNotifier _realtimeNotifier;
    private readonly IMediator _mediator;
    private readonly IPetRepository _petRepository;
    private readonly IChatReadStateRepository _readState;
    private readonly NotificationService _notificationService;
    private readonly Microsoft.Extensions.Logging.ILogger<MatchService> _logger;
    private readonly IUserPresenceTracker _presenceTracker;
    private readonly TrustScoreService _trustScoreService;

    public MatchService(
        ICurrentUserService currentUserService,
        IVoicePostRepository voicePostRepository,
        IVoiceCommentRepository voiceCommentRepository,
        IMatchConnectionRepository matchConnectionRepository,
        IUserRepository userRepository,
        IChatMessageRepository chatMessageRepository,
        IRealtimeNotifier realtimeNotifier,
        IMediator mediator,
        IPetRepository petRepository,
        IChatReadStateRepository readState,
        NotificationService notificationService,
        Microsoft.Extensions.Logging.ILogger<MatchService> logger,
        IUserPresenceTracker presenceTracker,
        TrustScoreService trustScoreService)
    {
        _currentUserService = currentUserService;
        _voicePostRepository = voicePostRepository;
        _voiceCommentRepository = voiceCommentRepository;
        _matchConnectionRepository = matchConnectionRepository;
        _userRepository = userRepository;
        _chatMessageRepository = chatMessageRepository;
        _realtimeNotifier = realtimeNotifier;
        _mediator = mediator;
        _petRepository = petRepository;
        _readState = readState;
        _notificationService = notificationService;
        _logger = logger;
        _presenceTracker = presenceTracker;
        _trustScoreService = trustScoreService;
    }

    public async Task<MatchCreatedResponseDto> CreateMatchAsync(CreateMatchRequest request, CancellationToken cancellationToken = default)
    {
        var posterId = _currentUserService.UserId;
        var user = await _userRepository.GetByIdAsync(posterId, cancellationToken);
        if (user != null && user.TrustScore < 20)
        {
            throw new ForbiddenApiException("Điểm tin cậy của bạn dưới mức 20. Tài khoản đã bị hạn chế ghép đôi.");
        }

        var post = await _voicePostRepository.GetByIdAsync(request.PostId, cancellationToken)
            ?? throw new NotFoundApiException("Voice post không tồn tại.");

        if (post.PosterId != _currentUserService.UserId)
        {
            throw new ForbiddenApiException("Bạn không được phép ghép đôi trên bài viết này.");
        }

        if (post.MatchCount >= post.MaxMatchSlots)
        {
            throw new ConflictApiException($"Bài Post này đã đạt giới hạn Match ({post.MaxMatchSlots} người). Hãy nâng cấp gói cước hoặc mua thêm Slot để Match tiếp!");
        }

        var comment = await _voiceCommentRepository.GetByIdAsync(request.CommentId, cancellationToken)
            ?? throw new NotFoundApiException("Bình luận bằng giọng nói không tồn tại.");

        if (await _matchConnectionRepository.AreMatchedAsync(post.PosterId, comment.CommenterId, cancellationToken))
        {
            throw new ConflictApiException("Bạn đã match với người dùng này rồi.");
        }

        if (comment.PostId != post.Id)
        {
            throw new ValidationApiException("Bình luận này không thuộc về bài đăng đã chọn.");
        }

        if (comment.Status != VoiceCommentStatus.Pending)
        {
            throw new ConflictApiException("Bình luận này đã được xử lý.");
        }

        var result = await _matchConnectionRepository.CreateConnectionAsync(post.Id, comment.Id, post.PosterId, cancellationToken);

        var payload = $"{{\"matchId\": \"{result.MatchConnection.Id}\"}}";
        
        await _notificationService.SendNotificationAsync(
            result.MatchConnection.UserBId, 
            NotificationType.Matching, 
            "Yêu cầu kết nối mới!", 
            "Có một người muốn kết nối với bạn.", 
            payload, cancellationToken);

        return new MatchCreatedResponseDto
        {
            MatchId = result.MatchConnection.Id,
            UserB_Id = result.MatchConnection.UserBId,
            Status = result.MatchConnection.Status.ToString(),
            PostClosed = result.PostClosed,
            ExpiresAt = result.MatchConnection.ExpiresAt
        };
    }

    public async Task AcceptMatchAsync(Guid matchId, CancellationToken cancellationToken = default)
    {
        var match = await _matchConnectionRepository.GetByIdAsync(matchId, cancellationToken)
            ?? throw new NotFoundApiException("Không tìm thấy yêu cầu ghép đôi.");

        if (match.UserBId != _currentUserService.UserId)
        {
            throw new ForbiddenApiException("Bạn không có quyền chấp nhận yêu cầu ghép đôi này.");
        }

        if (match.Status != MatchStatus.Pending)
        {
            throw new ConflictApiException("Yêu cầu ghép đôi này không ở trạng thái chờ.");
        }

        try
        {
            await _matchConnectionRepository.ExecuteInTransactionAsync(async () =>
            {
                _logger.LogInformation("Accept: updating status");
                await _matchConnectionRepository.UpdateStatusAsync(matchId, MatchStatus.Active, cancellationToken);
                match.Status = MatchStatus.Active;

                _logger.LogInformation("Accept: creating system message");
                var systemMessage = new ChatMessage
                {
                    Id = Guid.NewGuid().ToString("N")[..24], // 24 hex chars for MongoDB ObjectId
                    MatchId = match.Id,
                    SenderId = null,
                    MessageType = MessageType.System,
                    Content = "Hai bạn đã kết nối thành công.",
                    CreatedAt = DateTimeOffset.UtcNow
                };

                await _chatMessageRepository.AddAsync(systemMessage, cancellationToken);
                
                _logger.LogInformation("Accept: creating pet");
                await _mediator.Send(new CreatePetCommand(match.Id), cancellationToken);

                _logger.LogInformation("Accept: saving changes");
                // SaveChanges is handled internally by repository and MediatR

                try
                {
                    await _realtimeNotifier.NotifyMatchCreatedAsync(match, cancellationToken);
                    await _realtimeNotifier.NotifyNewMessageAsync(systemMessage, cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send SignalR notification for match {MatchId}.", match.Id);
                }
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Accept DB error. MatchId={MatchId}, Inner={Inner}", matchId, ex.InnerException?.Message);
            throw;
        }

        _logger.LogInformation("Accept: sending notification");
        // Send notifications
        var payload = $"{{\"matchId\": \"{match.Id}\"}}";
        
        await _notificationService.SendNotificationAsync(
            match.UserAId, 
            NotificationType.Matching, 
            "Kết nối thành công!", 
            "Người ấy đã chấp nhận yêu cầu kết nối của bạn.", 
            payload, cancellationToken);

        await _notificationService.SendNotificationAsync(
            match.UserAId, 
            NotificationType.Pet, 
            "Trứng Pet đã xuất hiện!", 
            "Pet của hai bạn đã được tạo, hãy vào chăm sóc nhé.", 
            payload, cancellationToken);

        await _notificationService.SendNotificationAsync(
            match.UserBId, 
            NotificationType.Pet, 
            "Trứng Pet đã xuất hiện!", 
            "Pet của hai bạn đã được tạo, hãy vào chăm sóc nhé.", 
            payload, cancellationToken);
    }

    public async Task RejectMatchAsync(Guid matchId, CancellationToken cancellationToken = default)
    {
        var match = await _matchConnectionRepository.GetByIdAsync(matchId, cancellationToken)
            ?? throw new NotFoundApiException("Không tìm thấy yêu cầu ghép đôi.");

        if (match.UserBId != _currentUserService.UserId)
        {
            throw new ForbiddenApiException("Bạn không có quyền từ chối yêu cầu ghép đôi này.");
        }

        if (match.Status != MatchStatus.Pending)
        {
            throw new ConflictApiException("Yêu cầu ghép đôi này không ở trạng thái chờ.");
        }

        await _matchConnectionRepository.UpdateStatusAsync(matchId, MatchStatus.Rejected, cancellationToken);
    }

    public async Task<IReadOnlyList<InboxItemDto>> GetInboxAsync(string? status, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        var matches = await _matchConnectionRepository.GetActiveByUserAsync(userId, cancellationToken);

        if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            return Array.Empty<InboxItemDto>();
        }

        var inboxItems = new List<InboxItemDto>();
        var matchIds = matches.Select(m => m.Id).ToList();
        var unreadMap = await _readState.CountUnreadByMatchesAsync(userId, matchIds, cancellationToken);
        
        var partnerIds = matches.Select(m => m.UserAId == userId ? m.UserBId : m.UserAId).Distinct().ToList();
        var onlineUsersMap = await _presenceTracker.GetOnlineUsersAsync(partnerIds);

        foreach (var match in matches)
        {
            var partnerId = match.UserAId == userId ? match.UserBId : match.UserAId;
            var partner = await _userRepository.GetByIdAsync(partnerId, cancellationToken);
            var lastMessageResult = await _chatMessageRepository.GetByMatchAsync(match.Id, cursor: null, limit: 1, cancellationToken);
            var lastMessage = lastMessageResult.Items.FirstOrDefault();

            var pet = await _petRepository.GetByMatchIdAsync(match.Id, cancellationToken);
            var petDto = pet is null
                ? new PetStateDto { Name = string.Empty, Hp = 80, Level = 0 }
                : new PetStateDto
                {
                    Name = pet.Name ?? string.Empty,
                    Hp = pet.Hp,
                    Level = (int)pet.Stage
                };

            var isPending = match.Status == MatchStatus.Pending;
            inboxItems.Add(new InboxItemDto
            {
                MatchId = match.Id,
                Partner = new PartnerPreviewDto
                {
                    Id = partnerId,
                    DisplayName = partner?.DisplayName ?? $"User #{partnerId.ToString()[..4]}",
                    AvatarUrl = partner?.AvatarUrl ?? "default_avatar.png",
                    IsOnline = onlineUsersMap.GetValueOrDefault(partnerId),
                    LastActiveAt = onlineUsersMap.GetValueOrDefault(partnerId) ? null : partner?.LastActiveAt
                },
                LastMessage = lastMessage == null
                    ? null
                    : new LastMessagePreviewDto
                    {
                        Type = lastMessage.MessageType.ToString(),
                        ContentUrl = lastMessage.ContentUrl,
                        Content = lastMessage.Content,
                        CreatedAt = lastMessage.CreatedAt
                    },
                UnreadCount = unreadMap.GetValueOrDefault(match.Id),
                PetState = petDto,
                Status = match.Status.ToString(),
                IsSender = match.UserAId == userId,
                ExpiresAt = match.ExpiresAt
            });
        }

        return inboxItems;
    }

    public async Task UnmatchAsync(Guid matchId, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        var match = await _matchConnectionRepository.GetByIdAsync(matchId, cancellationToken);
        if (match != null && match.Status != MatchStatus.Unmatched && match.CreatedAt > DateTimeOffset.UtcNow.AddMinutes(-10))
        {
            await _trustScoreService.DeductUnmatchPenaltyAsync(userId, cancellationToken);
        }

        var ok = await _matchConnectionRepository.UnmatchAsync(matchId, userId, cancellationToken);
        if (!ok)
            throw new ValidationApiException("Không thể hủy ghép đôi (unmatch) cuộc trò chuyện này.");

        var systemMessage = new ChatMessage
        {
            Id = Guid.NewGuid().ToString("N"),
            MatchId = matchId,
            SenderId = null,
            MessageType = MessageType.System,
            Content = "Một trong hai bạn đã hủy kết nối.",
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _chatMessageRepository.AddAsync(systemMessage, cancellationToken);
        await _realtimeNotifier.NotifyNewMessageAsync(systemMessage, cancellationToken: cancellationToken);
    }
}