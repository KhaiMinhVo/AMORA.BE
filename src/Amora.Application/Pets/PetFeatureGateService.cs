using Amora.Application.Exceptions;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;

namespace Amora.Application.Pets;

/// <summary>Enforce tính năng mở khóa theo stage Pet — server-side.</summary>
public sealed class PetFeatureGateService
{
    private readonly IPetRepository _pets;
    private readonly IMatchMediaUsageRepository _mediaUsage;

    public PetFeatureGateService(IPetRepository pets, IMatchMediaUsageRepository mediaUsage)
    {
        _pets = pets;
        _mediaUsage = mediaUsage;
    }

    public async Task ValidateSendAsync(Guid matchId, MessageType type, CancellationToken cancellationToken)
    {
        if (type is MessageType.Text or MessageType.Voice or MessageType.System)
            return;

        var pet = await _pets.GetByMatchIdAsync(matchId, cancellationToken)
            ?? throw new NotFoundApiException("Không tìm thấy thú cưng cho cuộc trò chuyện này.");

        var features = PetFeatureUnlocks.ForStage(pet.Stage).ToHashSet(StringComparer.OrdinalIgnoreCase);

        switch (type)
        {
            case MessageType.Image:
                if (!features.Contains("photo_once_per_day"))
                    throw new ForbiddenApiException("Gửi ảnh mở khóa ở giai đoạn Mầm Non (RP ≥ 200).");
                break;
            default:
                throw new ValidationApiException($"Loại tin nhắn '{type}' không được hỗ trợ.");
        }
    }

    public async Task RegisterImageSentAsync(Guid matchId, Guid userId, CancellationToken cancellationToken)
    {
        var usage = await _mediaUsage.GetTodayAsync(matchId, userId, cancellationToken);
        if (usage is not null && usage.ImagesSent >= 1)
            throw new ForbiddenApiException("Đã dùng hết lượt gửi ảnh hôm nay (1 lần/ngày).");

        await _mediaUsage.IncrementImageSentAsync(matchId, userId, cancellationToken);
    }

    public async Task<int> ValidateCallAsync(Guid matchId, string callType, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(callType))
            throw new ValidationApiException("Call type là bắt buộc.");

        var pet = await _pets.GetByMatchIdAsync(matchId, cancellationToken)
            ?? throw new NotFoundApiException("Không tìm thấy thú cưng cho cuộc trò chuyện này.");

        var features = PetFeatureUnlocks.ForStage(pet.Stage).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var normalizedType = callType.Trim().ToLowerInvariant();

        return normalizedType switch
        {
            "voice" or "audio" => features.Contains("voice_call_5min")
                ? 5 * 60
                : throw new ForbiddenApiException("Voice call mở khóa ở giai đoạn Thú Nhỏ."),
            "video" => features.Contains("video_call_3min")
                ? 3 * 60
                : throw new ForbiddenApiException("Video call mở khóa ở giai đoạn Thú Trưởng Thành."),
            _ => throw new ValidationApiException("Loại cuộc gọi không được hỗ trợ.")
        };
    }
}
