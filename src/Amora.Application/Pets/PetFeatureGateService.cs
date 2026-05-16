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
            ?? throw new NotFoundApiException("Pet not found for this match.");

        var features = PetFeatureUnlocks.ForStage(pet.Stage).ToHashSet(StringComparer.OrdinalIgnoreCase);

        switch (type)
        {
            case MessageType.Image:
                if (!features.Contains("photo_once_per_day"))
                    throw new ForbiddenApiException("Gửi ảnh mở khóa ở giai đoạn Mầm Non (RP ≥ 200).");
                break;
            default:
                throw new ValidationApiException($"Message type '{type}' is not supported.");
        }
    }

    public async Task RegisterImageSentAsync(Guid matchId, Guid userId, CancellationToken cancellationToken)
    {
        var usage = await _mediaUsage.GetTodayAsync(matchId, userId, cancellationToken);
        if (usage is not null && usage.ImagesSent >= 1)
            throw new ForbiddenApiException("Đã dùng hết lượt gửi ảnh hôm nay (1 lần/ngày).");

        await _mediaUsage.IncrementImageSentAsync(matchId, userId, cancellationToken);
    }
}
