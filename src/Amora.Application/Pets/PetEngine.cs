using System.Text.Json;
using Amora.Domain.Entities;
using Amora.Domain.Enums;

namespace Amora.Application.Pets;

/// <summary>Logic HP / Mood / RP / tiến hóa — không đọc nội dung chat.</summary>
public static class PetEngine
{
    public const int MaxHp = 100;
    public const int DailyHpGainCap = 30;
    public const int DailyTextRpCap = 100; // Cập nhật theo yêu cầu
    public const int DailyVoiceRpCap = 50;  // Giới hạn max là 50/ngày theo yêu cầu mới
    public const int DailyOnlineRp = 5;
    public const int HighHpStreakBonusRp = 20;

    public static void ResetDailyStatsIfNeeded(Pet pet)
    {
        var today = Amora.Application.Common.TimeHelper.GetVietnamToday();
        if (pet.RpStatsDate == today) return;

        pet.RpStatsDate = today;
        pet.RpFromTextToday = 0;
        pet.RpFromVoiceToday = 0;
        pet.OnlineBonusGrantedToday = false;
    }

    public static void ResetHpGainWindowIfNeeded(Pet pet)
    {
        if (DateTimeOffset.UtcNow - pet.HpGainWindowStart > TimeSpan.FromHours(24))
        {
            pet.HpGainWindowStart = DateTimeOffset.UtcNow;
            pet.HpGainedIn24h = 0;
        }
    }

    public static int ApplyDecay(Pet pet, DateTimeOffset now)
    {
        if (pet.IsFrozen || pet.IsDead) return 0;

        // Auto-fix zombie pets that reached max penalty but didn't freeze properly
        if (pet.IdlePenaltyStep >= 4)
        {
            pet.Hp = 0;
            pet.IsFrozen = true;
            pet.UpdatedAt = now;
            return 1; // Return > 0 to trigger database save in PetCoordinator
        }

        var idle = now - pet.LastInteractionAt;
        int totalLoss = 0;

        while (pet.IdlePenaltyStep < 4)
        {
            var nextStep = pet.IdlePenaltyStep + 1;
            int stepLoss = 0;

            if (nextStep == 1 && idle >= TimeSpan.FromHours(6))
                stepLoss = 20;
            else if (nextStep == 2 && idle >= TimeSpan.FromHours(12))
                stepLoss = 20;
            else if (nextStep == 3 && idle >= TimeSpan.FromHours(18))
                stepLoss = 30;
            else if (nextStep == 4 && idle >= TimeSpan.FromHours(24))
                stepLoss = 30;

            if (stepLoss == 0) break;

            totalLoss += stepLoss;
            pet.Mood = Math.Max(0, pet.Mood - 10);
            pet.IdlePenaltyStep = nextStep;
        }

        if (totalLoss > 0)
        {
            pet.Hp = Math.Max(0, pet.Hp - totalLoss);
            pet.UpdatedAt = now;

            if (pet.Hp == 0)
            {
                pet.IsFrozen = true;
            }
        }

        return totalLoss;
    }

    public static int ApplyHpGain(Pet pet, int rawGain, bool bypassCap = false)
    {
        if (pet.IsFrozen && !bypassCap) return 0;

        ResetHpGainWindowIfNeeded(pet);

        var allowed = bypassCap
            ? rawGain
            : Math.Min(rawGain, Math.Max(0, DailyHpGainCap - pet.HpGainedIn24h));

        if (allowed <= 0) return 0;

        pet.Hp = Math.Min(MaxHp, pet.Hp + allowed);
        pet.HpGainedIn24h += allowed;
        pet.LastInteractionAt = DateTimeOffset.UtcNow;
        pet.IdlePenaltyStep = 0;
        pet.UpdatedAt = DateTimeOffset.UtcNow;

        if (pet.Hp > 0) pet.IsFrozen = false;

        return allowed;
    }

    public static int ComputeHpFromInteraction(int messageCount, double voiceMinutes, int vibeScore)
    {
        return Math.Max(0, messageCount + (int)Math.Floor(voiceMinutes * 3) + vibeScore);
    }



    public static int AwardTextRp(Pet pet)
    {
        if (pet.IsFrozen) return 0;
        ResetDailyStatsIfNeeded(pet);
        if (pet.RpFromTextToday >= DailyTextRpCap) return 0;

        // Cập nhật: 2 RP/tin
        var gain = 2;
        // Đảm bảo không vượt quá cap
        if (pet.RpFromTextToday + gain > DailyTextRpCap)
        {
            gain = DailyTextRpCap - pet.RpFromTextToday;
        }

        pet.Rp += gain;
        pet.RpFromTextToday += gain;
        return gain;
    }

    public static int AwardVoiceRp(Pet pet, double durationSeconds)
    {
        if (pet.IsFrozen) return 0;
        ResetDailyStatsIfNeeded(pet);
        if (pet.RpFromVoiceToday >= DailyVoiceRpCap) return 0;

        var multiplier = HasActiveBuff(pet, PetBuffType.DoubleVoiceRp) ? 2 : 1;
        var gain = 2 * multiplier;

        if (pet.RpFromVoiceToday + gain > DailyVoiceRpCap)
        {
            gain = DailyVoiceRpCap - pet.RpFromVoiceToday;
        }

        pet.Rp += gain;
        pet.RpFromVoiceToday += gain;
        return gain;
    }

    public static int AwardOnlineRp(Pet pet)
    {
        if (pet.IsFrozen) return 0;
        ResetDailyStatsIfNeeded(pet);
        if (pet.OnlineBonusGrantedToday) return 0;

        pet.Rp += DailyOnlineRp;
        pet.OnlineBonusGrantedToday = true;
        return DailyOnlineRp;
    }

    public static void RecordDailyHpSnapshot(Pet pet)
    {
        var today = Amora.Application.Common.TimeHelper.GetVietnamToday();
        if (pet.LastHpSnapshotDate == today) return;

        if (pet.LastHpSnapshotDate.HasValue && pet.HpSnapshotCount > 0)
        {
            var avg = pet.HpSnapshotSum / pet.HpSnapshotCount;
            pet.ConsecutiveHighHpDays = avg >= 70 ? pet.ConsecutiveHighHpDays + 1 : 0;

            if (pet.ConsecutiveHighHpDays >= 3)
            {
                pet.Rp += HighHpStreakBonusRp;
                pet.ConsecutiveHighHpDays = 0;
            }
        }

        pet.LastHpSnapshotDate = today;
        pet.HpSnapshotSum = pet.Hp;
        pet.HpSnapshotCount = 1;
    }

    public static GrowthStage EvaluateStage(Pet pet)
    {
        // Điều kiện tiến hoá: dùng RP (EXP) và Mood thay cho HP theo tài liệu thiết kế
        if (pet.Rp >= 5000 && pet.Mood >= 80) return GrowthStage.Legend; // Trưởng thành cuối cùng
        if (pet.Rp >= 2500 && pet.Mood >= 70) return GrowthStage.Adult;  // Trưởng thành
        if (pet.Rp >= 1400 && pet.Mood >= 65) return GrowthStage.Young;   // Thiếu niên
        if (pet.Rp >= 600 && pet.Mood >= 60) return GrowthStage.Sprout;  // Thú non

        return GrowthStage.ResonanceSeed; // Trứng
    }

    public static bool HasActiveBuff(Pet pet, PetBuffType buffType)
    {
        var buffs = DeserializeBuffs(pet.ActiveBuffsJson);
        return buffs.Any(b => b.Type == buffType && b.ExpiresAt > DateTimeOffset.UtcNow);
    }

    public static void AddBuff(Pet pet, PetBuffType type, TimeSpan duration)
    {
        var buffs = DeserializeBuffs(pet.ActiveBuffsJson);
        buffs.RemoveAll(b => b.Type == type);
        buffs.Add(new PetBuffState(type, DateTimeOffset.UtcNow.Add(duration)));
        pet.ActiveBuffsJson = JsonSerializer.Serialize(buffs);
    }

    public static List<PetBuffState> DeserializeBuffs(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<PetBuffState>();
        return JsonSerializer.Deserialize<List<PetBuffState>>(json) ?? new List<PetBuffState>();
    }

    public sealed record PetBuffState(PetBuffType Type, DateTimeOffset ExpiresAt);
}
