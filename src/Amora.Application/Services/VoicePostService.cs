using Amora.Application.Abstractions;
using Amora.Application.Dtos.Posts;
using Amora.Application.Exceptions;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;
using Amora.Application.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MediatR;
using Amora.Application.Posts.Commands;

namespace Amora.Application.Services;

public sealed class VoicePostService
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IVoicePostRepository _voicePostRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPostReactionRepository _postReactionRepository;
    private readonly NotificationService _notificationService;
    private readonly AudioProcessingService _audioProcessingService;
    private readonly string? _storageBucketName;
    private readonly Microsoft.Extensions.DependencyInjection.IServiceScopeFactory _scopeFactory;
    private readonly Microsoft.Extensions.Logging.ILogger<VoicePostService> _logger;
    private readonly TrustScoreService _trustScoreService;

    public VoicePostService(
        ICurrentUserService currentUserService,
        IVoicePostRepository voicePostRepository,
        IUserRepository userRepository,
        IPostReactionRepository postReactionRepository,
        NotificationService notificationService,
        AudioProcessingService audioProcessingService,
        IConfiguration configuration,
        Microsoft.Extensions.DependencyInjection.IServiceScopeFactory scopeFactory,
        Microsoft.Extensions.Logging.ILogger<VoicePostService> logger,
        TrustScoreService trustScoreService)
    {
        _currentUserService = currentUserService;
        _voicePostRepository = voicePostRepository;
        _userRepository = userRepository;
        _postReactionRepository = postReactionRepository;
        _notificationService = notificationService;
        _audioProcessingService = audioProcessingService;
        _storageBucketName = configuration["Storage:BucketName"] ?? configuration["AWS:BucketName"];
        _scopeFactory = scopeFactory;
        _logger = logger;
        _trustScoreService = trustScoreService;
    }

    public async Task LogPlayAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        var log = new AudioPlayLog
        {
            Id = Guid.NewGuid(),
            UserId = _currentUserService.UserId,
            PostId = postId,
            PlayedAt = DateTimeOffset.UtcNow
        };
        
        using var scope = _scopeFactory.CreateScope();
        var audioLogRepo = scope.ServiceProvider.GetRequiredService<IAudioPlayLogRepository>();
        await audioLogRepo.AddAsync(log, cancellationToken);
    }

    public async Task<CreateVoicePostResponseDto> CreateAsync(CreateVoicePostRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.AudioUrl))
        {
            throw new ValidationApiException("Vui lòng cung cấp file ghi âm (AudioUrl).");
        }

        var userId = _currentUserService.UserId;
        var since = new DateTimeOffset(DateTimeOffset.UtcNow.UtcDateTime.Date, TimeSpan.Zero);
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundApiException("Không tìm thấy người dùng.");

        if (user.TrustScore < 50)
        {
            throw new ForbiddenApiException("Điểm tin cậy (Trust Score) của bạn quá thấp để đăng bài. Vui lòng cải thiện bằng cách hoạt động tích cực hơn.");
        }

        int maxMatchSlots = user.SubscriptionType switch
        {
            SubscriptionType.Gold => 8,
            SubscriptionType.Premium => 5,
            _ => 3
        };

        var todayCount = await _voicePostRepository.CountByPosterSinceAsync(userId, since, cancellationToken);

        if (todayCount >= 3 && user.SubscriptionType == SubscriptionType.Free)
        {
            throw new ConflictApiException("Bạn đã đạt giới hạn đăng 3 bài Voice Post trong ngày hôm nay. Hãy nâng cấp gói Premium/Gold để đăng không giới hạn.");
        }

        var post = new VoicePost
        {
            Id = Guid.NewGuid(),
            PosterId = userId,
            AudioUrl = request.AudioUrl,
            MatchCount = 0,
            MaxMatchSlots = maxMatchSlots,
            Status = VoicePostStatus.Processing, // Worker sẽ chuyển sang Open sau khi xử lý xong
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _voicePostRepository.AddAsync(post, cancellationToken);
        await _trustScoreService.AddVoicePostBonusAsync(userId, cancellationToken);

        // Trích xuất S3 file key từ publicUrl (bỏ phần domain, giữ lại path)
        // Ví dụ: "https://amora-voice-bucket.s3.amazonaws.com/voices/abc.m4a" → "voices/abc.m4a"
        var s3FileKey = ExtractS3KeyFromUrl(post.AudioUrl, _storageBucketName);
        try
        {
            await _audioProcessingService.EnqueueAudioProcessingAsync(post.Id, s3FileKey, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enqueue audio processing for voice post {PostId}.", post.Id);
        }

        // Run AI Moderation in the background
        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var aiModeration = scope.ServiceProvider.GetRequiredService<AiModerationService>();
            var postRepo = scope.ServiceProvider.GetRequiredService<IVoicePostRepository>();
            var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var userBanRepo = scope.ServiceProvider.GetRequiredService<IUserBanRepository>();
            var adminNotifier = scope.ServiceProvider.GetRequiredService<AdminNotificationService>();

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
                        
                        var ban = new Amora.Domain.Entities.UserBan
                        {
                            Id = Guid.NewGuid(),
                            UserId = u.Id,
                            BanReason = "[AI AUTOMATED] Voice post contained toxic/offensive language.",
                            BannedUntil = DateTimeOffset.UtcNow.AddDays(7),
                            CreatedAt = DateTimeOffset.UtcNow,
                            IsActive = true
                        };

                        await userRepo.UpdateAsync(u);
                        await userBanRepo.AddAsync(ban);
                        
                        await adminNotifier.NotifyAutoBlockedContentAsync("Voice Post", "Chứa nội dung vi phạm/toxic.", userId);
                    }
                }
            }

            try
            {
                var scopedMediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                await scopedMediator.Send(new AnalyzeVoiceToneCommand(post.Id));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to dispatch AnalyzeVoiceToneCommand for post {PostId}", post.Id);
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
        var postIds = items.Select(x => x.Id).ToList();
        var userReactions = await _postReactionRepository.GetUserReactionsAsync(_currentUserService.UserId, postIds, cancellationToken);
        
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
                    DisplayName = poster?.DisplayName ?? $"User #{post.PosterId.ToString()[..4]}",
                    AvatarUrl = poster?.AvatarUrl ?? "default_avatar.png"
                },
                AudioUrl = post.AudioUrl,
                MatchCount = post.MatchCount,
                Status = post.Status.ToString(),
                CreatedAt = post.CreatedAt,
                IsBoosted = post.IsBoosted,
                MaxMatchSlots = post.MaxMatchSlots,
                ReactionCount = post.ReactionCount,
                UserReactionType = userReactions.TryGetValue(post.Id, out var type) ? type.ToString() : null
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
        var postIds = items.Select(x => x.Id).ToList();
        var userReactions = await _postReactionRepository.GetUserReactionsAsync(_currentUserService.UserId, postIds, cancellationToken);
        
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
                    DisplayName = poster?.DisplayName ?? $"User #{post.PosterId.ToString()[..4]}",
                    AvatarUrl = poster?.AvatarUrl ?? "default_avatar.png"
                },
                AudioUrl = post.AudioUrl,
                MatchCount = post.MatchCount,
                Status = post.Status.ToString(),
                CreatedAt = post.CreatedAt,
                IsBoosted = post.IsBoosted,
                MaxMatchSlots = post.MaxMatchSlots,
                ReactionCount = post.ReactionCount,
                UserReactionType = userReactions.TryGetValue(post.Id, out var type) ? type.ToString() : null
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
            ?? throw new NotFoundApiException("Voice post không tồn tại.");

        if (post.PosterId != _currentUserService.UserId)
        {
            throw new ForbiddenApiException("Bạn không có quyền đóng bài viết này.");
        }

        if (post.Status == VoicePostStatus.Closed)
        {
            return;
        }

        post.Status = VoicePostStatus.Closed;
        await _voicePostRepository.UpdateAsync(post, cancellationToken);
    }

    public async Task<ReactToPostResponse> ReactToPostAsync(Guid postId, ReactToPostRequest request, CancellationToken cancellationToken = default)
    {
        var post = await _voicePostRepository.GetByIdAsync(postId, cancellationToken)
            ?? throw new NotFoundApiException("Voice post không tồn tại.");

        var userId = _currentUserService.UserId;
        var existingReaction = await _postReactionRepository.GetReactionAsync(postId, userId, cancellationToken);

        if (existingReaction != null)
        {
            if (existingReaction.Type == request.ReactionType)
            {
                // Unlike if same reaction
                await _postReactionRepository.DeleteAsync(existingReaction, cancellationToken);
                post.ReactionCount--;
                await _voicePostRepository.UpdateAsync(post, cancellationToken);
                
                return new ReactToPostResponse { NewReactionCount = post.ReactionCount, CurrentReactionType = null };
            }
            else
            {
                // Change reaction
                existingReaction.Type = request.ReactionType;
                await _postReactionRepository.UpdateAsync(existingReaction, cancellationToken);
                
                return new ReactToPostResponse { NewReactionCount = post.ReactionCount, CurrentReactionType = request.ReactionType.ToString() };
            }
        }
        else
        {
            // New reaction
            var newReaction = new PostReaction
            {
                Id = Guid.NewGuid(),
                PostId = postId,
                UserId = userId,
                Type = request.ReactionType,
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _postReactionRepository.AddAsync(newReaction, cancellationToken);
            
            post.ReactionCount++;
            await _voicePostRepository.UpdateAsync(post, cancellationToken);
            
            if (post.PosterId != userId)
            {
                var reactor = await _userRepository.GetByIdAsync(userId, cancellationToken);
                var reactorName = reactor?.DisplayName ?? "Một người dùng";
                await _notificationService.SendNotificationAsync(
                    post.PosterId,
                    NotificationType.VoiceFeed,
                    $"{reactorName} đã thả cảm xúc về bài đăng của bạn!",
                    $"{reactorName} vừa thả {request.ReactionType} vào bài đăng Voice của bạn.",
                    $"{{\"postId\": \"{post.Id}\"}}",
                    cancellationToken
                );
            }
            
            return new ReactToPostResponse { NewReactionCount = post.ReactionCount, CurrentReactionType = request.ReactionType.ToString() };
        }
    }
}