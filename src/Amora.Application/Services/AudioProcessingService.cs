using Amora.Application.Abstractions;
using Amora.Application.Dtos.Posts;
using Amora.Application.Exceptions;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;

namespace Amora.Application.Services;

/// <summary>
/// Xử lý nghiệp vụ liên quan đến VoicePost.
/// Sau khi tạo Post thành công sẽ bắn message vào RabbitMQ để Python Worker xử lý âm thanh.
/// </summary>
public sealed class AudioProcessingService
{
    private readonly IVoicePostRepository _voicePostRepository;
    private readonly IMessageBus _messageBus;
    private readonly IRealtimeNotifier _realtimeNotifier;

    public AudioProcessingService(
        IVoicePostRepository voicePostRepository,
        IMessageBus messageBus,
        IRealtimeNotifier realtimeNotifier)
    {
        _voicePostRepository = voicePostRepository;
        _messageBus = messageBus;
        _realtimeNotifier = realtimeNotifier;
    }

    /// <summary>
    /// Được gọi từ VoicePostService ngay sau khi tạo Post —
    /// đặt trạng thái Processing và gửi task vào hàng đợi.
    /// </summary>
    public async Task EnqueueAudioProcessingAsync(Guid postId, string s3FileKey, CancellationToken cancellationToken = default)
    {
        await _messageBus.PublishAsync(
            taskName: "tasks.process_voice_post",
            args: [postId.ToString(), s3FileKey],
            cancellationToken
        );
    }

    /// <summary>
    /// Nhận kết quả từ Python Worker qua Webhook và cập nhật DB.
    /// </summary>
    public async Task HandleAudioProcessedAsync(AudioProcessedPayload payload, CancellationToken cancellationToken = default)
    {
        var post = await _voicePostRepository.GetByIdAsync(payload.PostId, cancellationToken)
            ?? throw new NotFoundApiException($"Post {payload.PostId} không tồn tại.");

        if (payload.Status == "Success" && payload.PetVibeData is not null && !string.IsNullOrEmpty(payload.CleanAudioUrl))
        {
            // Cập nhật URL âm thanh sang file đã lọc nhiễu
            post.AudioUrl = payload.CleanAudioUrl;
            post.Status = VoicePostStatus.Open; // Bây giờ mới hiện lên Feed

            // Gắn dữ liệu Pet vào Post
            post.PetVibeData = new PetVibeData
            {
                Id = Guid.NewGuid(),
                PostId = post.Id,
                Energy = payload.PetVibeData.Energy,
                Pitch = payload.PetVibeData.Pitch,
                PitchVariance = payload.PetVibeData.PitchVariance,
                IsMonotone = payload.PetVibeData.IsMonotone,
                DurationSec = payload.PetVibeData.DurationSec,
                CleanAudioUrl = payload.CleanAudioUrl,
                ProcessedAt = DateTimeOffset.UtcNow,
            };
        }
        else
        {
            post.Status = VoicePostStatus.Failed;
        }

        await _voicePostRepository.UpdateAsync(post, cancellationToken);
    }
}
