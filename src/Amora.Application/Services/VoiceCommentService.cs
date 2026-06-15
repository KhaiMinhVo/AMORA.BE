using Amora.Application.Abstractions;
using Amora.Application.Dtos.Comments;
using Amora.Application.Exceptions;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Amora.Application.Services;

public sealed class VoiceCommentService
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IVoicePostRepository _voicePostRepository;
    private readonly IVoiceCommentRepository _voiceCommentRepository;
    private readonly IUserRepository _userRepository;
    private readonly Microsoft.Extensions.DependencyInjection.IServiceScopeFactory _scopeFactory;
    private readonly NotificationService _notificationService;

    public VoiceCommentService(
        ICurrentUserService currentUserService,
        IVoicePostRepository voicePostRepository,
        IVoiceCommentRepository voiceCommentRepository,
        IUserRepository userRepository,
        Microsoft.Extensions.DependencyInjection.IServiceScopeFactory scopeFactory,
        NotificationService notificationService)
    {
        _currentUserService = currentUserService;
        _voicePostRepository = voicePostRepository;
        _voiceCommentRepository = voiceCommentRepository;
        _userRepository = userRepository;
        _scopeFactory = scopeFactory;
        _notificationService = notificationService;
    }

    public async Task<CreateCommentResponseDto> CreateCommentAsync(Guid postId, CreateVoiceCommentRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.AudioUrl))
        {
            throw new ValidationApiException("AudioUrl is required.");
        }

        if (request.Duration <= 0)
        {
            throw new ValidationApiException("Duration must be greater than zero.");
        }

        var post = await _voicePostRepository.GetByIdAsync(postId, cancellationToken)
            ?? throw new NotFoundApiException("Voice post not found.");

        if (post.Status == VoicePostStatus.Closed)
        {
            throw new ConflictApiException("This voice post is already closed.");
        }

        var userId = _currentUserService.UserId;
        if (await _voiceCommentRepository.HasUserCommentedOnPostAsync(userId, postId, cancellationToken))
        {
            throw new ConflictApiException("You have already commented on this post.");
        }

        var comment = new VoiceComment
        {
            Id = Guid.NewGuid(),
            PostId = postId,
            CommenterId = userId,
            AudioUrl = request.AudioUrl,
            Duration = request.Duration,
            Status = VoiceCommentStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _voiceCommentRepository.AddAsync(comment, cancellationToken);
        
        if (post.PosterId != userId)
        {
            await _notificationService.SendNotificationAsync(
                post.PosterId,
                NotificationType.VoiceFeed,
                "Có người vừa phản hồi bài đăng của bạn!",
                "Một người dùng vừa để lại lời nhắn cho Voice Post của bạn.",
                $"{{\"postId\": \"{post.Id}\"}}",
                cancellationToken
            );
        }

        // Run AI Moderation in the background for comments
        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var aiModeration = scope.ServiceProvider.GetRequiredService<AiModerationService>();
            var commentRepo = scope.ServiceProvider.GetRequiredService<IVoiceCommentRepository>();
            var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var userBanRepo = scope.ServiceProvider.GetRequiredService<IUserBanRepository>();
            var adminNotifier = scope.ServiceProvider.GetRequiredService<AdminNotificationService>();

            var text = await aiModeration.TranscribeAudioAsync(request.AudioUrl);
            if (!string.IsNullOrWhiteSpace(text))
            {
                bool isToxic = await aiModeration.IsMessageToxicAsync(text);
                if (isToxic)
                {
                    // Ban user and close comment
                    var c = await commentRepo.GetByIdAsync(comment.Id);
                    if (c != null)
                    {
                        c.Status = VoiceCommentStatus.Rejected;
                        await commentRepo.UpdateAsync(c);
                    }

                    var u = await userRepo.GetByIdAsync(userId);
                    if (u != null)
                    {
                        u.IsBanned = true;
                        
                        var ban = new Amora.Domain.Entities.UserBan
                        {
                            Id = Guid.NewGuid(),
                            UserId = u.Id,
                            BanReason = "[AI AUTOMATED] Voice comment contained toxic/offensive language.",
                            BannedUntil = DateTimeOffset.UtcNow.AddDays(7),
                            CreatedAt = DateTimeOffset.UtcNow,
                            IsActive = true
                        };

                        await userRepo.UpdateAsync(u);
                        await userBanRepo.AddAsync(ban);
                        
                        await adminNotifier.NotifyAutoBlockedContentAsync("Voice Comment", "Chứa nội dung vi phạm/toxic.", userId);
                    }
                }
            }
        });

        return new CreateCommentResponseDto
        {
            CommentId = comment.Id,
            Status = comment.Status.ToString(),
            CreatedAt = comment.CreatedAt
        };
    }

    public async Task<VoiceCommentListResponseDto> GetCommentsAsync(Guid postId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var post = await _voicePostRepository.GetByIdAsync(postId, cancellationToken)
            ?? throw new NotFoundApiException("Voice post not found.");

        // Đã gỡ bỏ check: Mọi người đều có thể xem danh sách comment của bài post này (giống Facebook)

        var (items, totalCount) = await _voiceCommentRepository.GetPagedByPostIdAsync(postId, page, pageSize, cancellationToken);
        var resultItems = new List<VoiceCommentItemDto>();

        foreach (var comment in items)
        {
            var commenter = await _userRepository.GetByIdAsync(comment.CommenterId, cancellationToken);
            
            resultItems.Add(new VoiceCommentItemDto
            {
                CommentId = comment.Id,
                Commenter = new CommenterPreviewDto
                {
                    Id = comment.CommenterId,
                    DisplayName = commenter?.DisplayName ?? $"User #{comment.CommenterId.ToString()[..4]}",
                    AvatarUrl = commenter?.AvatarUrl ?? "default_avatar.png"
                },
                AudioUrl = comment.AudioUrl,
                Duration = comment.Duration,
                Status = comment.Status.ToString(),
                CreatedAt = comment.CreatedAt
            });
        }

        return new VoiceCommentListResponseDto
        {
            TotalCount = totalCount,
            Items = resultItems
        };
    }

    public async Task DeleteCommentAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        var comment = await _voiceCommentRepository.GetByIdAsync(commentId, cancellationToken)
            ?? throw new NotFoundApiException("Voice comment not found.");

        var post = await _voicePostRepository.GetByIdAsync(comment.PostId, cancellationToken);
        
        bool isCommenter = comment.CommenterId == _currentUserService.UserId;
        bool isPoster = post?.PosterId == _currentUserService.UserId;

        if (!isCommenter && !isPoster)
        {
            throw new ForbiddenApiException("You do not have permission to delete this comment.");
        }

        await _voiceCommentRepository.DeleteAsync(comment, cancellationToken);
    }
}