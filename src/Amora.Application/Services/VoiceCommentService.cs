using Amora.Application.Abstractions;
using Amora.Application.Dtos.Comments;
using Amora.Application.Exceptions;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Amora.Application.Services;

public sealed class VoiceCommentService
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IVoicePostRepository _voicePostRepository;
    private readonly IVoiceCommentRepository _voiceCommentRepository;
    private readonly IUserRepository _userRepository;
    private readonly Microsoft.Extensions.DependencyInjection.IServiceScopeFactory _scopeFactory;
    private readonly NotificationService _notificationService;
    private readonly Microsoft.Extensions.Logging.ILogger<VoiceCommentService> _logger;

    public VoiceCommentService(
        ICurrentUserService currentUserService,
        IVoicePostRepository voicePostRepository,
        IVoiceCommentRepository voiceCommentRepository,
        IUserRepository userRepository,
        Microsoft.Extensions.DependencyInjection.IServiceScopeFactory scopeFactory,
        NotificationService notificationService,
        Microsoft.Extensions.Logging.ILogger<VoiceCommentService> logger)
    {
        _currentUserService = currentUserService;
        _voicePostRepository = voicePostRepository;
        _voiceCommentRepository = voiceCommentRepository;
        _userRepository = userRepository;
        _scopeFactory = scopeFactory;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task LogPlayAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        var log = new AudioPlayLog
        {
            Id = Guid.NewGuid(),
            UserId = _currentUserService.UserId,
            CommentId = commentId,
            PlayedAt = DateTimeOffset.UtcNow
        };
        
        using var scope = _scopeFactory.CreateScope();
        var audioLogRepo = scope.ServiceProvider.GetRequiredService<IAudioPlayLogRepository>();
        await audioLogRepo.AddAsync(log, cancellationToken);
    }

    public async Task<CreateCommentResponseDto> CreateCommentAsync(Guid postId, CreateVoiceCommentRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.AudioUrl))
        {
            throw new ValidationApiException("Vui lòng cung cấp file ghi âm (AudioUrl).");
        }

        if (request.Duration <= 0)
        {
            throw new ValidationApiException("Thời lượng ghi âm phải lớn hơn 0.");
        }

        var post = await _voicePostRepository.GetByIdAsync(postId, cancellationToken)
            ?? throw new NotFoundApiException("Voice post không tồn tại.");

        if (post.Status == VoicePostStatus.Closed)
        {
            throw new ConflictApiException("Bài Voice Post này đã đóng.");
        }

        var userId = _currentUserService.UserId;

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user != null && user.TrustScore < 50)
        {
            throw new ForbiddenApiException("Điểm tin cậy của bạn dưới mức 50. Tài khoản đã bị hạn chế bình luận.");
        }

        if (await _voiceCommentRepository.HasUserCommentedOnPostAsync(userId, postId, cancellationToken))
        {
            throw new ConflictApiException("Bạn đã bình luận vào bài viết này rồi.");
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
            var actorName = user?.DisplayName ?? "Người dùng Amora";
            var actorAvatar = user?.AvatarUrl ?? "default_avatar.png";
            
            await _notificationService.SendNotificationAsync(
                post.PosterId,
                NotificationType.VoiceFeed,
                "Có người vừa phản hồi bài đăng của bạn!",
                "Một người dùng vừa để lại lời nhắn cho Voice Post của bạn.",
                $"{{\"postId\":\"{post.Id}\",\"actorName\":\"{actorName}\",\"actorAvatar\":\"{actorAvatar}\"}}",
                cancellationToken
            );
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var aiModeration = scope.ServiceProvider.GetRequiredService<AiModerationService>();
            var commentRepo = scope.ServiceProvider.GetRequiredService<IVoiceCommentRepository>();
            var adminNotifier = scope.ServiceProvider.GetRequiredService<AdminNotificationService>();

            var text = await aiModeration.TranscribeAudioAsync(request.AudioUrl, cancellationToken);
            var isToxic = !string.IsNullOrWhiteSpace(text)
                && await aiModeration.IsMessageToxicAsync(text, cancellationToken);
            var current = await commentRepo.GetByIdAsync(comment.Id, cancellationToken);
            if (current != null)
            {
                current.Status = isToxic ? VoiceCommentStatus.Rejected : VoiceCommentStatus.Accepted;
                await commentRepo.UpdateAsync(current, cancellationToken);
                comment.Status = current.Status;

                if (isToxic)
                {
                    await adminNotifier.NotifyAutoBlockedContentAsync(
                        "Voice Comment",
                        "AI đã ẩn nội dung nghi vi phạm; cần quản trị viên xem xét.",
                        userId,
                        cancellationToken);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI moderation failed for voice comment {CommentId}.", comment.Id);
            comment.Status = VoiceCommentStatus.Accepted;
            await _voiceCommentRepository.UpdateAsync(comment, cancellationToken);
        }

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
            ?? throw new NotFoundApiException("Voice post không tồn tại.");

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
            ?? throw new NotFoundApiException("Bình luận bằng giọng nói không tồn tại.");

        var post = await _voicePostRepository.GetByIdAsync(comment.PostId, cancellationToken);
        
        bool isCommenter = comment.CommenterId == _currentUserService.UserId;
        bool isPoster = post?.PosterId == _currentUserService.UserId;

        if (!isCommenter && !isPoster)
        {
            throw new ForbiddenApiException("Bạn không có quyền xóa bình luận này.");
        }

        await _voiceCommentRepository.DeleteAsync(comment, cancellationToken);
    }
}
