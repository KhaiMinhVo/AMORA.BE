using Amora.Application.Abstractions;
using Amora.Application.Dtos.Comments;
using Amora.Application.Exceptions;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;

namespace Amora.Application.Services;

public sealed class VoiceCommentService
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IVoicePostRepository _voicePostRepository;
    private readonly IVoiceCommentRepository _voiceCommentRepository;

    public VoiceCommentService(
        ICurrentUserService currentUserService,
        IVoicePostRepository voicePostRepository,
        IVoiceCommentRepository voiceCommentRepository)
    {
        _currentUserService = currentUserService;
        _voicePostRepository = voicePostRepository;
        _voiceCommentRepository = voiceCommentRepository;
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

        if (post.PosterId != _currentUserService.UserId)
        {
            throw new ForbiddenApiException("You are not allowed to view this private queue.");
        }

        var (items, totalCount) = await _voiceCommentRepository.GetPagedByPostIdAsync(postId, page, pageSize, cancellationToken);

        return new VoiceCommentListResponseDto
        {
            TotalCount = totalCount,
            Items = items.Select(comment => new VoiceCommentItemDto
            {
                CommentId = comment.Id,
                Commenter = new CommenterPreviewDto
                {
                    Id = comment.CommenterId,
                    DisplayName = $"Ẩn danh #{comment.CommenterId.ToString()[..4]}",
                    AvatarUrl = "default_blur.png"
                },
                AudioUrl = comment.AudioUrl,
                Duration = comment.Duration,
                Status = comment.Status.ToString(),
                CreatedAt = comment.CreatedAt
            }).ToList()
        };
    }
}