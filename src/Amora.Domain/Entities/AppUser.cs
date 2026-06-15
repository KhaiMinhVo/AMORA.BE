using Amora.Domain.Enums;

namespace Amora.Domain.Entities;

public sealed class AppUser
{
    public Guid Id { get; set; }

    public string? Email { get; set; }

    public string? PasswordHash { get; set; }
    
    public string? GoogleId { get; set; }

    public string? PhoneNumber { get; set; }

    public bool RequiresPasswordUpdate { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string AvatarUrl { get; set; } = string.Empty;

    public string Role { get; set; } = "User";

    public string[] Photos { get; set; } = [];

    // ── Moderation ──────────────────────────────────────────────────────
    // Navigation property for user bans
    public ICollection<UserBan> Bans { get; set; } = new List<UserBan>();
    public bool IsBanned { get; set; }

    // ── Profile mở rộng ─────────────────────────────────────────────────

    public DateOnly? DateOfBirth { get; set; }

    public Gender Gender { get; set; } = Gender.PreferNotToSay;

    public TargetGender TargetGender { get; set; } = TargetGender.Both;

    /// <summary>Thành phố / khu vực.</summary>
    public string? City { get; set; }

    /// <summary>Giới thiệu ngắn về bản thân (tối đa 300 ký tự).</summary>
    public string? Bio { get; set; }

    /// <summary>File âm thanh giới thiệu bản thân.</summary>
    public string? VoiceIntroUrl { get; set; }

    /// <summary>Thời lượng của file âm thanh giới thiệu (tính bằng giây).</summary>
    public int? VoiceIntroDuration { get; set; }

    /// <summary>Danh sách sở thích, phân tách bởi dấu phẩy. Ví dụ: "Nhạc,Phim,Du lịch"</summary>
    public string? Interests { get; set; }

    /// <summary>True nếu user đã điền đầy đủ thông tin Onboarding.</summary>
    public bool IsProfileComplete { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastActiveAt { get; set; }

    // ── Economy (Tiền tệ duy nhất) ──────────────────────────────────────
    public int Diamonds { get; set; }

    public DateOnly? LastDiamondRewardDate { get; set; }

    public DateOnly? LastCoPresenceCoinDate { get; set; }

    // ── Subscriptions ───────────────────────────────────────────────────
    public SubscriptionType SubscriptionType { get; set; } = SubscriptionType.Free;
    
    public DateTimeOffset? SubscriptionEndDate { get; set; }

    public bool HasActiveSubscription(SubscriptionType type)
    {
        return SubscriptionType == type && SubscriptionEndDate.HasValue && SubscriptionEndDate.Value > DateTimeOffset.UtcNow;
    }
}
