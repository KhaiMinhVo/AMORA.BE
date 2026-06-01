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

    public PetCoordinator(
        IPetRepository petRepository,
        IMatchConnectionRepository matchRepository,
        IMessagePublisher messagePublisher,
        IPetRealtimeNotifier petNotifier,
        IChatMessageRepository chatMessageRepository)
    {
        _petRepository = petRepository;
        _matchRepository = matchRepository;
        _messagePublisher = messagePublisher;
        _petNotifier = petNotifier;
        _chatMessageRepository = chatMessageRepository;
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
        Rp = pet.Rp,
        Stage = (int)pet.Stage,
        StageName = PetFeatureUnlocks.StageDisplayName(pet.Stage),
        Type = pet.Type.ToString(),
        TypeName = PetFeatureUnlocks.PetTypeName(pet.Type),
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

        if (oldStage == GrowthStage.ResonanceSeed && pet.Stage >= GrowthStage.Sprout && pet.Type == PetType.None)
        {
            pet.Type = await EvaluateInitiativeBalanceAsync(pet.MatchId, cancellationToken);
        }
    }

    private async Task<PetType> EvaluateInitiativeBalanceAsync(Guid matchId, CancellationToken cancellationToken)
    {
        var match = await _matchRepository.GetByIdAsync(matchId, cancellationToken);
        if (match == null) return PetType.Dog;

        var result = await _chatMessageRepository.GetByMatchAsync(matchId, null, 500, cancellationToken);
        var messages = result.Items.Where(x => x.MessageType == MessageType.Text).ToList();
        
        if (messages.Count == 0) return PetType.Dog;

        int countA = 0, countB = 0, lenA = 0, lenB = 0;
        var responseTimes = new List<double>();
        Guid? lastSender = null;
        DateTimeOffset? lastMsgTime = null;

        foreach (var msg in messages.OrderBy(m => m.CreatedAt))
        {
            if (msg.SenderId == match.UserAId)
            {
                countA++;
                lenA += msg.Content?.Length ?? 0;
            }
            else if (msg.SenderId == match.UserBId)
            {
                countB++;
                lenB += msg.Content?.Length ?? 0;
            }

            if (lastSender != null && lastSender != msg.SenderId && lastMsgTime != null)
            {
                var delay = (msg.CreatedAt - lastMsgTime.Value).TotalSeconds;
                if (delay >= 0) responseTimes.Add(delay);
            }

            lastSender = msg.SenderId;
            lastMsgTime = msg.CreatedAt;
        }

        double avgResponseTime = responseTimes.Count > 0 ? responseTimes.Average() : 30;
        double avgLen = messages.Count > 0 ? (double)(lenA + lenB) / messages.Count : 0;
        double ratioA = countA + countB > 0 ? (double)countA / (countA + countB) : 0.5;

        if (avgResponseTime <= 15) return PetType.Rabbit;
        if (avgResponseTime > 60 && avgLen > 30) return PetType.Bear;
        if (ratioA > 0.65 || ratioA < 0.35) return PetType.Cat;
        return PetType.Dog;
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
