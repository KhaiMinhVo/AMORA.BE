using Amora.Application.Abstractions;
using Amora.Application.Dtos.Posts;
using Amora.Application.Exceptions;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;

namespace Amora.Application.Services;

public sealed class VoicePostService
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IVoicePostRepository _voicePostRepository;
    private readonly IUserRepository _userRepository;

    public VoicePostService(
        ICurrentUserService currentUserService,
        IVoicePostRepository voicePostRepository,
        IUserRepository userRepository)
    {
        _currentUserService = currentUserService;
        _voicePostRepository = voicePostRepository;
        _userRepository = userRepository;
    }

    public async Task<CreateVoicePostResponseDto> CreateAsync(CreateVoicePostRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.AudioUrl))
        {
            throw new ValidationApiException("AudioUrl is required.");
        }

        var userId = _currentUserService.UserId;
        var since = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        var todayCount = await _voicePostRepository.CountByPosterSinceAsync(userId, since, cancellationToken);

        if (todayCount >= 3)
        {
            throw new ConflictApiException("You have reached the daily limit of 3 voice posts.");
        }

        var post = new VoicePost
        {
            Id = Guid.NewGuid(),
            PosterId = userId,
            AudioUrl = request.AudioUrl,
            MatchCount = 0,
            Status = VoicePostStatus.Open,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _voicePostRepository.AddAsync(post, cancellationToken);

        return new CreateVoicePostResponseDto
        {
            PostId = post.Id,
            PosterId = post.PosterId,
            AudioUrl = post.AudioUrl,
            MatchCount = post.MatchCount,
            Status = post.Status.ToString(),
            CreatedAt = post.CreatedAt
        };
    }

    public async Task<FeedResponseDto> GetFeedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var (items, totalCount) = await _voicePostRepository.GetFeedPageAsync(_currentUserService.UserId, page, pageSize, cancellationToken);
        var feedItems = new List<FeedPostItemDto>();

        foreach (var post in items)
        {
            var poster = await _userRepository.GetByIdAsync(post.PosterId, cancellationToken);

            feedItems.Add(new FeedPostItemDto
            {
                PostId = post.Id,
                Poster = new PosterPreviewDto
                {
                    Id = post.PosterId,
                    DisplayName = poster?.DisplayName ?? $"Ẩn danh #{post.PosterId.ToString()[..4]}",
                    AvatarUrl = poster?.AvatarUrl ?? "default_avatar.png"
                },
                AudioUrl = post.AudioUrl,
                MatchCount = post.MatchCount,
                Status = post.Status.ToString(),
                CreatedAt = post.CreatedAt
            });
        }

        return new FeedResponseDto
        {
            TotalCount = totalCount,
            Items = feedItems
        };
    }

    public async Task CloseAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        var post = await _voicePostRepository.GetByIdAsync(postId, cancellationToken)
            ?? throw new NotFoundApiException("Voice post not found.");

        if (post.PosterId != _currentUserService.UserId)
        {
            throw new ForbiddenApiException("You are not allowed to close this post.");
        }

        if (post.Status == VoicePostStatus.Closed)
        {
            return;
        }

        post.Status = VoicePostStatus.Closed;
        await _voicePostRepository.UpdateAsync(post, cancellationToken);
    }
}