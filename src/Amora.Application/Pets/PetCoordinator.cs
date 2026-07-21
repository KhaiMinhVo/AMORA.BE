using System.Text.Json;
using Amora.Application.Abstractions;
using Amora.Application.Dtos.Pets;
using Amora.Application.Messaging;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Amora.Application.Tests")]

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
    private readonly IRealtimeNotifier _realtimeNotifier;

    public PetCoordinator(
        IPetRepository petRepository,
        IMatchConnectionRepository matchRepository,
        IMessagePublisher messagePublisher,
        IPetRealtimeNotifier petNotifier,
        IChatMessageRepository chatMessageRepository,
        Services.NotificationService notificationService,
        IRealtimeNotifier realtimeNotifier)
    {
        _petRepository = petRepository;
        _matchRepository = matchRepository;
        _messagePublisher = messagePublisher;
        _petNotifier = petNotifier;
        _chatMessageRepository = chatMessageRepository;
        _notificationService = notificationService;
        _realtimeNotifier = realtimeNotifier;
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

    public async Task ProcessVoiceMessageAsync(Guid matchId, Guid senderId, double durationSeconds, CancellationToken cancellationToken)
    {
        var pet = await RequirePetAsync(matchId, cancellationToken);
        if (pet.IsFrozen) return;

        var gain = PetEngine.AwardVoiceRp(pet, durationSeconds);
        
        var replyDelay = ComputeReplyDelayMinutes(pet, senderId);
        await EvaluateStageAndTypeAsync(pet, cancellationToken);
        pet.UpdatedAt = DateTimeOffset.UtcNow;
        pet.LastPartnerMessageAt = DateTimeOffset.UtcNow;

        if (gain > 0)
        {
            await LogAsync(pet, "VoiceInteraction", new { senderId, durationSeconds, gain }, cancellationToken);
            await SaveAndNotifyAsync(pet, cancellationToken);
        }
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

        var replyDelay = ComputeReplyDelayMinutes(pet, result.UserId);
        await EvaluateStageAndTypeAsync(pet, cancellationToken);
        pet.LastInteractionAt = DateTimeOffset.UtcNow;
        pet.IdlePenaltyStep = 0;
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

    public static PetStatusDto ToDto(Pet pet)
    {
        var today = Amora.Application.Common.TimeHelper.GetVietnamToday();
        var claimsToday = pet.WaterClaimDate == today ? pet.WaterClaimsToday : 0;
        DateTimeOffset? nextWaterClaim = null;
        if (claimsToday < 3 && pet.LastWaterClaimAt.HasValue)
        {
            var next = pet.LastWaterClaimAt.Value.AddHours(1);
            if (next > DateTimeOffset.UtcNow)
                nextWaterClaim = next;
        }

        return new PetStatusDto
        {
            PetId = pet.Id,
            Name = pet.Name ?? string.Empty,
            MatchId = pet.MatchId,
            Hp = pet.Hp,
            Mood = pet.Mood,
            Rp = pet.Rp,
            VoiceExpToday = pet.RpFromVoiceToday,
            MaxVoiceExpPerDay = 50,
            Stage = (int)pet.Stage,
            StageName = PetFeatureUnlocks.StageDisplayName(pet.Stage),
            Type = pet.Type.ToString(),
            TypeName = PetFeatureUnlocks.PetTypeName(pet.Type),
            TypeDescription = PetFeatureUnlocks.PetTypeDescription(pet.Type),
            IsFrozen = pet.IsFrozen,
            ExpiresAtHint = pet.LastInteractionAt.AddHours(6),
            UnlockedFeatures = PetFeatureUnlocks.ForStage(pet.Stage),
            WaterClaimCountToday = claimsToday,
            MaxWaterClaimsPerDay = 3,
            NextWaterClaimAvailableAt = nextWaterClaim
        };
    }

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
        await CheckAndTriggerMoodMessageAsync(pet, cancellationToken);
        await _petRepository.SaveChangesAsync(cancellationToken);
        await NotifyAsync(pet, cancellationToken);
    }

    private async Task CheckAndTriggerMoodMessageAsync(Pet pet, CancellationToken cancellationToken)
    {
        var today = Amora.Application.Common.TimeHelper.GetVietnamToday();
        if (pet.LastMoodMessageDate == today) return; // Chỉ gửi 1 lần mỗi ngày để tránh spam

        string? moodMessage = null;

        if (pet.Mood <= 20)
        {
            moodMessage = "Hai bạn cãi nhau hay bơ mình làm mình buồn quá 😿";
        }
        else if (pet.Mood >= 80)
        {
            moodMessage = "Hôm nay hai bạn dễ thương ghê 🥰";
        }

        if (moodMessage != null)
        {
            // Insert System Message into Chat
            var sysMsg = new ChatMessage
            {
                Id = Guid.NewGuid().ToString("N")[..24], // 24 hex chars for MongoDB ObjectId
                MatchId = pet.MatchId,
                SenderId = null, // System
                MessageType = MessageType.System,
                Content = moodMessage,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _chatMessageRepository.AddAsync(sysMsg, cancellationToken);
            await _realtimeNotifier.NotifyNewMessageAsync(sysMsg, cancellationToken: cancellationToken);

            pet.LastMoodMessageDate = today;
        }
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

        if (pet.Stage >= GrowthStage.Sprout && pet.Type == PetType.None)
        {
            pet.Type = await EvaluateChronotypeAsync(pet.MatchId, cancellationToken);
        }
    }

    internal Task<PetType> EvaluateChronotypeAsync(Guid matchId, CancellationToken cancellationToken)
    {
        // Yêu cầu mới: Quả trứng nở ra random 1 trong 4 loại thú cưng
        var randomType = (PetType)Random.Shared.Next(1, 5);
        return Task.FromResult(randomType);
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
