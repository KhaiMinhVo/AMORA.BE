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

    public PetCoordinator(
        IPetRepository petRepository,
        IMatchConnectionRepository matchRepository,
        IMessagePublisher messagePublisher,
        IPetRealtimeNotifier petNotifier)
    {
        _petRepository = petRepository;
        _matchRepository = matchRepository;
        _messagePublisher = messagePublisher;
        _petNotifier = petNotifier;
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
            Mood = PetMood.Neutral,
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
        pet.Mood = PetEngine.ComputeMood(pet, vibeScore: 0, replyDelay);
        pet.Stage = PetEngine.EvaluateStage(pet);
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

        PetEngine.RegisterVibe(pet, result.VibeScore);

        var voiceMinutes = result.DurationSeconds / 60.0;
        var rawHp = PetEngine.ComputeHpFromInteraction(1, voiceMinutes, result.VibeScore);
        PetEngine.ApplyHpGain(pet, rawHp);
        PetEngine.AwardVoiceRp(pet, result.DurationSeconds);

        var replyDelay = ComputeReplyDelayMinutes(pet, result.UserId);
        pet.Mood = PetEngine.ComputeMood(pet, result.VibeScore, replyDelay);
        pet.Stage = PetEngine.EvaluateStage(pet);
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
                pet.Stage = PetEngine.EvaluateStage(pet);
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
        Mood = pet.Mood.ToString(),
        Rp = pet.Rp,
        Stage = (int)pet.Stage,
        StageName = PetFeatureUnlocks.StageDisplayName(pet.Stage),
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
