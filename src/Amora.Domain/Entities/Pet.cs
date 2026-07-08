using Amora.Domain.Common;
using Amora.Domain.Enums;

namespace Amora.Domain.Entities;

/// <summary>Thú cưng ảo gắn 1:1 với một match — dùng chung bởi cả hai user.</summary>
public sealed class Pet : BaseEntity
{
    public Guid MatchId { get; set; }

    public int Hp { get; set; } = 80;

    public int Mood { get; set; } = 50;

    public DateOnly? LastMoodMessageDate { get; set; }

    public long Rp { get; set; }

    public GrowthStage Stage { get; set; } = GrowthStage.ResonanceSeed;

    public PetType Type { get; set; } = PetType.None;

    /// <summary>HP = 0 → đóng băng, không nhận RP.</summary>
    public bool IsFrozen { get; set; }

    public PetEmotion CurrentEmotion { get; set; } = PetEmotion.Neutral;

    /// <summary>Số lượng tin nhắn mới chưa được phân tích cảm xúc.</summary>
    public int UnanalyzedMessageCount { get; set; }

    public DateTimeOffset LastInteractionAt { get; set; } = DateTimeOffset.UtcNow;
    
    /// <summary>Bước phạt hiện tại (0: 0h, 1: 6h, 2: 12h, 3: 18h, 4: 24h).</summary>
    public int IdlePenaltyStep { get; set; }

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

    /// <summary>Tên thú cưng (đặt bằng Rename Card).</summary>
    public string? Name { get; set; }

    /// <summary>Pet đã chết (24h+ không nhắn tin và chưa dùng Revive).</summary>
    public bool IsDead { get; set; }

    /// <summary>Thời điểm Pet chết.</summary>
    public DateTimeOffset? DeathTime { get; set; }

    /// <summary>Số lần nhận nước miễn phí trong ngày (tối đa 3).</summary>
    public int WaterClaimsToday { get; set; }

    /// <summary>Thời điểm nhận nước gần nhất (cooldown 1 tiếng).</summary>
    public DateTimeOffset? LastWaterClaimAt { get; set; }

    /// <summary>Ngày reset số lần nhận nước.</summary>
    public DateOnly WaterClaimDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>JSON danh sách phụ kiện đang mặc [{"slotId":"hat","itemId":"guid","code":"non_ca_tim"}].</summary>
    public string? EquippedCosmeticsJson { get; set; }



    public MatchConnection? Match { get; set; }
}
