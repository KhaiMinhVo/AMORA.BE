using Amora.Application.Abstractions;
using Amora.Application.Dtos.Posts;
using Amora.Application.Exceptions;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Amora.Application.Services;

public sealed class VoicePostService
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IVoicePostRepository _voicePostRepository;
    private readonly IUserRepository _userRepository;
    private readonly AudioProcessingService _audioProcessingService;
    private readonly string? _storageBucketName;
    private readonly Microsoft.Extensions.DependencyInjection.IServiceScopeFactory _scopeFactory;

    public VoicePostService(
        ICurrentUserService currentUserService,
        IVoicePostRepository voicePostRepository,
        IUserRepository userRepository,
        AudioProcessingService audioProcessingService,
        IConfiguration configuration,
        Microsoft.Extensions.DependencyInjection.IServiceScopeFactory scopeFactory)
    {
        _currentUserService = currentUserService;
        _voicePostRepository = voicePostRepository;
        _userRepository = userRepository;
        _audioProcessingService = audioProcessingService;
        _storageBucketName = configuration["Storage:BucketName"] ?? configuration["AWS:BucketName"];
        _scopeFactory = scopeFactory;
    }

    public async Task<CreateVoicePostResponseDto> CreateAsync(CreateVoicePostRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.AudioUrl))
        {
            throw new ValidationApiException("AudioUrl is required.");
        }

        var userId = _currentUserService.UserId;
        var since = DateTimeOffset.UtcNow.Date;
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
            Status = VoicePostStatus.Processing, // Worker sẽ chuyển sang Open sau khi xử lý xong
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _voicePostRepository.AddAsync(post, cancellationToken);

        // Trích xuất S3 file key từ publicUrl (bỏ phần domain, giữ lại path)
        // Ví dụ: "https://amora-voice-bucket.s3.amazonaws.com/voices/abc.m4a" → "voices/abc.m4a"
        var s3FileKey = ExtractS3KeyFromUrl(post.AudioUrl, _storageBucketName);
        await _audioProcessingService.EnqueueAudioProcessingAsync(post.Id, s3FileKey, cancellationToken);

        // Run AI Moderation in the background
        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var aiModeration = scope.ServiceProvider.GetRequiredService<AiModerationService>();
            var postRepo = scope.ServiceProvider.GetRequiredService<IVoicePostRepository>();
            var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

            var text = await aiModeration.TranscribeAudioAsync(request.AudioUrl);
            if (!string.IsNullOrWhiteSpace(text))
            {
                bool isToxic = await aiModeration.IsMessageToxicAsync(text);
                if (isToxic)
                {
                    // Ban user and close post
                    var p = await postRepo.GetByIdAsync(post.Id);
                    if (p != null)
                    {
                        p.Status = VoicePostStatus.Closed;
                        await postRepo.UpdateAsync(p);
                    }

                    var u = await userRepo.GetByIdAsync(userId);
                    if (u != null)
                    {
                        u.IsBanned = true;
                        u.BannedUntil = DateTimeOffset.UtcNow.AddDays(7);
                        u.BanReason = "[AI AUTOMATED] Voice post contained toxic/offensive language.";
                        await userRepo.UpdateAsync(u);
                    }
                }
            }
        });

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

    /// <summary>
    /// Trích xuất S3 key từ full URL.
    /// Input:  "https://amora-voice-bucket.s3.amazonaws.com/voices/uuid.m4a"
    /// Output: "voices/uuid.m4a"
    /// </summary>
    private static string ExtractS3KeyFromUrl(string url, string? bucketName)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            // uri.AbsolutePath = "/voices/uuid.m4a" → bỏ dấu "/" đầu tiên
            var path = uri.AbsolutePath.TrimStart('/');
            if (!string.IsNullOrWhiteSpace(bucketName) && path.StartsWith(bucketName + "/", StringComparison.OrdinalIgnoreCase))
            {
                return path[(bucketName.Length + 1)..];
            }

            return path;
        }

        // Fallback: nếu url đã là key rồi (không phải full URL)
        return url;
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
                Id = post.Id,
                Poster = new PosterPreviewDto
                {
                    Id = post.PosterId,
                    DisplayName = poster?.DisplayName ?? $"Ẩn danh #{post.PosterId.ToString()[..4]}",
                    AvatarUrl = "anonymous.png"
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

    public async Task<FeedResponseDto> GetMyPostsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var (items, totalCount) = await _voicePostRepository.GetMyPostsPageAsync(_currentUserService.UserId, page, pageSize, cancellationToken);
        var feedItems = new List<FeedPostItemDto>();

        foreach (var post in items)
        {
            var poster = await _userRepository.GetByIdAsync(post.PosterId, cancellationToken);

            feedItems.Add(new FeedPostItemDto
            {
                Id = post.Id,
                Poster = new PosterPreviewDto
                {
                    Id = post.PosterId,
                    DisplayName = poster?.DisplayName ?? $"Ẩn danh #{post.PosterId.ToString()[..4]}",
                    AvatarUrl = "anonymous.png"
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