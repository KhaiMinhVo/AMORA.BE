using Amora.Application.Abstractions;
using Amora.Application.Dtos.Matches;
using Amora.Application.Exceptions;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;

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

    public MatchService(
        ICurrentUserService currentUserService,
        IVoicePostRepository voicePostRepository,
        IVoiceCommentRepository voiceCommentRepository,
        IMatchConnectionRepository matchConnectionRepository,
        IUserRepository userRepository,
        IChatMessageRepository chatMessageRepository,
        IRealtimeNotifier realtimeNotifier)
    {
        _currentUserService = currentUserService;
        _voicePostRepository = voicePostRepository;
        _voiceCommentRepository = voiceCommentRepository;
        _matchConnectionRepository = matchConnectionRepository;
        _userRepository = userRepository;
        _chatMessageRepository = chatMessageRepository;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<MatchCreatedResponseDto> CreateMatchAsync(CreateMatchRequest request, CancellationToken cancellationToken = default)
    {
        var post = await _voicePostRepository.GetByIdAsync(request.PostId, cancellationToken)
            ?? throw new NotFoundApiException("Voice post not found.");

        if (post.PosterId != _currentUserService.UserId)
        {
            throw new ForbiddenApiException("You are not allowed to match on this post.");
        }

        var comment = await _voiceCommentRepository.GetByIdAsync(request.CommentId, cancellationToken)
            ?? throw new NotFoundApiException("Voice comment not found.");

        if (comment.PostId != post.Id)
        {
            throw new ValidationApiException("This comment does not belong to the selected post.");
        }

        if (comment.Status != VoiceCommentStatus.Pending)
        {
            throw new ConflictApiException("This comment has already been processed.");
        }

        var result = await _matchConnectionRepository.CreateConnectionAsync(post.Id, comment.Id, post.PosterId, cancellationToken);

        var systemMessage = new ChatMessage
        {
            Id = Guid.NewGuid().ToString("N"),
            MatchId = result.MatchConnection.Id,
            SenderId = null,
            MessageType = MessageType.System,
            Content = $"Hai bạn đã kết nối thành công từ bài Post {post.Id}.",
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _chatMessageRepository.AddAsync(systemMessage, cancellationToken);
        await _realtimeNotifier.NotifyMatchCreatedAsync(result.MatchConnection, cancellationToken);
        await _realtimeNotifier.NotifyNewMessageAsync(systemMessage, cancellationToken);

        return new MatchCreatedResponseDto
        {
            MatchId = result.MatchConnection.Id,
            UserB_Id = result.MatchConnection.UserBId,
            Status = result.MatchConnection.Status.ToString(),
            PostClosed = result.PostClosed
        };
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

        foreach (var match in matches)
        {
            var partnerId = match.UserAId == userId ? match.UserBId : match.UserAId;
            var partner = await _userRepository.GetByIdAsync(partnerId, cancellationToken);
            var lastMessageResult = await _chatMessageRepository.GetByMatchAsync(match.Id, cursor: null, limit: 1, cancellationToken);
            var lastMessage = lastMessageResult.Items.FirstOrDefault();

            inboxItems.Add(new InboxItemDto
            {
                MatchId = match.Id,
                Partner = new PartnerPreviewDto
                {
                    Id = partnerId,
                    DisplayName = partner?.DisplayName ?? $"Ẩn danh #{partnerId.ToString()[..4]}",
                    AvatarUrl = partner?.AvatarUrl ?? "default_avatar.png"
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
                UnreadCount = 0,
                PetState = new PetStateDto
                {
                    Hp = 95,
                    Mood = "Excited",
                    Level = 1
                }
            });
        }

        return inboxItems;
    }
}