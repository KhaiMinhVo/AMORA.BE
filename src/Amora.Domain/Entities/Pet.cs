using Amora.Domain.Common;
using Amora.Domain.Enums;

namespace Amora.Domain.Entities;

/// <summary>Thú cưng ảo gắn 1:1 với một match — dùng chung bởi cả hai user.</summary>
public sealed class Pet : BaseEntity
{
    public Guid MatchId { get; set; }

    public int Hp { get; set; } = 80;

    public long Rp { get; set; }

    public GrowthStage Stage { get; set; } = GrowthStage.ResonanceSeed;

    /// <summary>HP = 0 → đóng băng, không nhận RP.</summary>
    public bool IsFrozen { get; set; }

    public DateTimeOffset LastInteractionAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastPartnerMessageAt { get; set; }

    /// <summary>HP đã tăng trong cửa sổ 24h (giới hạn 30).</summary>
    public int HpGainedIn24h { get; set; }

    public DateTimeOffset HpGainWindowStart { get; set; } = DateTimeOffset.UtcNow;

    public DateOnly RpStatsDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public int RpFromTextToday { get; set; }

    public int RpFromVoiceToday { get; set; }

    public bool OnlineBonusGrantedToday { get; set; }

    /// <summary>Ngày liên tiếp HP trung bình ≥ 70 (thưởng +20 RP).</summary>
    public int ConsecutiveHighHpDays { get; set; }

    public DateOnly? LastHpSnapshotDate { get; set; }

    public double HpSnapshotSum { get; set; }

    public int HpSnapshotCount { get; set; }

    /// <summary>JSON danh sách buff đang active.</summary>
    public string? ActiveBuffsJson { get; set; }

    public MatchConnection? Match { get; set; }
}
