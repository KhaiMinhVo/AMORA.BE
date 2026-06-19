using System.Text.Json;
using Amora.Application.Abstractions;
using Amora.Application.Dtos.Pets;
using Amora.Application.Messaging;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;

namespace Amora.Application.Pets;

/// <summary>Điều phối thay đổi Pet — dùng bởi MediatR handlers và ChatService.</summary>
public sealed class PetCoordinator
{
    private readonly IPetRepository _petRepository;
    private readonly IMatchConnectionRepository _matchRepository;
    private readonly IMessagePublisher _messagePublisher;
    private readonly IPetRealtimeNotifier _petNotifier;
    private readonly IChatMessageRepository _chatMessageRepository;
    private readonly Services.NotificationService _notificationService;

    public PetCoordinator(
        IPetRepository petRepository,
        IMatchConnectionRepository matchRepository,
        IMessagePublisher messagePublisher,
        IPetRealtimeNotifier petNotifier,
        IChatMessageRepository chatMessageRepository,
        Services.NotificationService notificationService)
    {
        _petRepository = petRepository;
        _matchRepository = matchRepository;
        _messagePublisher = messagePublisher;
        _petNotifier = petNotifier;
        _chatMessageRepository = chatMessageRepository;
        _notificationService = notificationService;
    }

    public async Task<Pet> CreateForMatchAsync(Guid matchId, CancellationToken cancellationToken)
    {
        var existing = await _petRepository.GetByMatchIdAsync(matchId, cancellationToken);
        if (existing is not null) return existing;

        var pet = new Pet
        {
            Id = Guid.NewGuid(),
            MatchId = matchId,
            Hp = 80,
            Rp = 0,
            Stage = GrowthStage.ResonanceSeed,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _petRepository.AddAsync(pet, cancellationToken);
        await LogAsync(pet, "Created", new { matchId }, cancellationToken);
        await _petRepository.SaveChangesAsync(cancellationToken);
        await NotifyAsync(pet, cancellationToken);
        return pet;
    }

    public async Task ProcessTextMessageAsync(Guid matchId, Guid senderId, CancellationToken cancellationToken)
    {
        var pet = await RequirePetAsync(matchId, cancellationToken);
        if (pet.IsFrozen) return;

        var rawHp = PetEngine.ComputeHpFromInteraction(1, 0, 0);
        PetEngine.ApplyHpGain(pet, rawHp);
        PetEngine.AwardTextRp(pet);

        var replyDelay = ComputeReplyDelayMinutes(pet, senderId);
        await EvaluateStageAndTypeAsync(pet, cancellationToken);
        pet.UpdatedAt = DateTimeOffset.UtcNow;
        pet.LastPartnerMessageAt = DateTimeOffset.UtcNow;

        await LogAsync(pet, "TextInteraction", new { senderId, rawHp }, cancellationToken);
        await SaveAndNotifyAsync(pet, cancellationToken);
    }

    public async Task PublishVoiceForVibeAsync(
        Guid matchId,
        Guid userId,
        string audioUrl,
        double durationSeconds,
        CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        await _messagePublisher.PublishAsync(
            "chat_voice_processed",
            new ChatVoiceProcessedMessage
            {
                CorrelationId = correlationId,
                MatchId = matchId,
                UserId = userId,
                AudioUrl = audioUrl,
                DurationSeconds = durationSeconds
            },
            cancellationToken);
    }

    public async Task ProcessVibeResultAsync(ChatVibeResultMessage result, CancellationToken cancellationToken)
    {
        var pet = await RequirePetAsync(result.MatchId, cancellationToken);

        var voiceMinutes = result.DurationSeconds / 60.0;
        var rawHp = PetEngine.ComputeHpFromInteraction(1, voiceMinutes, result.VibeScore);
        PetEngine.ApplyHpGain(pet, rawHp);
        PetEngine.AwardVoiceRp(pet, result.DurationSeconds);

        var replyDelay = ComputeReplyDelayMinutes(pet, result.UserId);
        await EvaluateStageAndTypeAsync(pet, cancellationToken);
        pet.LastInteractionAt = DateTimeOffset.UtcNow;
        pet.UpdatedAt = DateTimeOffset.UtcNow;

        await LogAsync(pet, "VoiceVibe", result, cancellationToken);
        await SaveAndNotifyAsync(pet, cancellationToken);
    }

    public async Task<int> ApplyDecayBatchAsync(CancellationToken cancellationToken)
    {
        var pets = await _petRepository.GetPetsNeedingDecayAsync(100, cancellationToken);
        var count = 0;

        foreach (var pet in pets)
        {
            var loss = PetEngine.ApplyDecay(pet, DateTimeOffset.UtcNow);
            if (loss > 0)
            {
                count++;
                await EvaluateStageAndTypeAsync(pet, cancellationToken);
                await LogAsync(pet, "Decay", new { loss }, cancellationToken);
                await SaveAndNotifyAsync(pet, cancellationToken);
            }
        }

        return count;
    }

    public static PetStatusDto ToDto(Pet pet) => new()
    {
        PetId = pet.Id,
        MatchId = pet.MatchId,
        Hp = pet.Hp,
        Mood = pet.Mood,
        Energy = pet.Energy,
        Rp = pet.Rp,
        VoiceExpToday = pet.RpFromVoiceToday,
        MaxVoiceExpPerDay = 100,
        Stage = (int)pet.Stage,
        StageName = PetFeatureUnlocks.StageDisplayName(pet.Stage),
        Type = pet.Type.ToString(),
        TypeName = PetFeatureUnlocks.PetTypeName(pet.Type),
        TypeDescription = PetFeatureUnlocks.PetTypeDescription(pet.Type),
        IsFrozen = pet.IsFrozen,
        ExpiresAtHint = pet.LastInteractionAt.AddHours(6),
        UnlockedFeatures = PetFeatureUnlocks.ForStage(pet.Stage)
    };

    private async Task<Pet> RequirePetAsync(Guid matchId, CancellationToken cancellationToken)
    {
        return await _petRepository.GetByMatchIdAsync(matchId, cancellationToken)
            ?? throw new InvalidOperationException($"Pet not found for match {matchId}.");
    }

    private static double? ComputeReplyDelayMinutes(Pet pet, Guid senderId)
    {
        if (pet.LastPartnerMessageAt is null) return null;
        return (DateTimeOffset.UtcNow - pet.LastPartnerMessageAt.Value).TotalMinutes;
    }

    private async Task SaveAndNotifyAsync(Pet pet, CancellationToken cancellationToken)
    {
        await _petRepository.SaveChangesAsync(cancellationToken);
        await NotifyAsync(pet, cancellationToken);
    }

    private async Task NotifyAsync(Pet pet, CancellationToken cancellationToken)
    {
        var match = await _matchRepository.GetByIdAsync(pet.MatchId, cancellationToken);
        if (match is not null)
            await _petNotifier.NotifyPetStatusUpdatedAsync(pet, match, cancellationToken);
    }

    private async Task EvaluateStageAndTypeAsync(Pet pet, CancellationToken cancellationToken)
    {
        var oldStage = pet.Stage;
        pet.Stage = PetEngine.EvaluateStage(pet);

        if (oldStage != pet.Stage)
        {
            var match = await _matchRepository.GetByIdAsync(pet.MatchId, cancellationToken);
            if (match != null)
            {
                var payload = $"{{\"matchId\": \"{match.Id}\"}}";
                var body = $"Pet của hai bạn đã tiến hóa lên cấp độ mới: {PetFeatureUnlocks.StageDisplayName(pet.Stage)}!";
                
                await _notificationService.SendNotificationAsync(match.UserAId, NotificationType.Pet, "Pet đã tiến hóa! 🌟", body, payload, cancellationToken);
                await _notificationService.SendNotificationAsync(match.UserBId, NotificationType.Pet, "Pet đã tiến hóa! 🌟", body, payload, cancellationToken);
            }
        }

        if (oldStage == GrowthStage.ResonanceSeed && pet.Stage >= GrowthStage.Sprout && pet.Type == PetType.None)
        {
            pet.Type = await EvaluateChronotypeAsync(pet.MatchId, cancellationToken);
        }
    }

    private async Task<PetType> EvaluateChronotypeAsync(Guid matchId, CancellationToken cancellationToken)
    {
        var match = await _matchRepository.GetByIdAsync(matchId, cancellationToken);
        if (match == null) return PetType.Dog;

        var result = await _chatMessageRepository.GetByMatchAsync(matchId, null, 500, cancellationToken);
        var messages = result.Items.Where(x => x.MessageType == MessageType.Voice).ToList();
        
        if (messages.Count == 0) return PetType.Dog;

        int morningCount = 0;   // 05:00 - 11:59 (Dog)
        int daytimeCount = 0;   // 12:00 - 21:59 Weekdays (Rabbit)
        int nightCount = 0;     // 22:00 - 04:59 (Cat)
        int weekendCount = 0;   // Saturday, Sunday (Otter)

        foreach (var msg in messages)
        {
            // Convert UTC to local time (GMT+7)
            var localTime = msg.CreatedAt.ToOffset(TimeSpan.FromHours(7));

            if (localTime.DayOfWeek == DayOfWeek.Saturday || localTime.DayOfWeek == DayOfWeek.Sunday)
            {
                weekendCount++;
            }
            else
            {
                if (localTime.Hour >= 5 && localTime.Hour < 12)
                {
                    morningCount++;
                }
                else if (localTime.Hour >= 12 && localTime.Hour < 22)
                {
                    daytimeCount++;
                }
                else
                {
                    // 22:00 to 04:59
                    nightCount++;
                }
            }
        }

        var maxCount = Math.Max(Math.Max(morningCount, daytimeCount), Math.Max(nightCount, weekendCount));

        if (maxCount == morningCount) return PetType.Dog;
        if (maxCount == nightCount) return PetType.Cat;
        if (maxCount == weekendCount) return PetType.Otter;
        return PetType.Rabbit;
    }

    private async Task LogAsync(Pet pet, string eventType, object payload, CancellationToken cancellationToken)
    {
        await _petRepository.AddHistoryAsync(new PetStateHistory
        {
            Id = Guid.NewGuid(),
            PetId = pet.Id,
            EventType = eventType,
            PayloadJson = JsonSerializer.Serialize(payload),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);
    }
}
