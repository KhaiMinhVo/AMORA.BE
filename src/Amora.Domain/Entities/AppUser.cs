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
    public bool IsBanned { get; set; }
    public DateTimeOffset? BannedUntil { get; set; }
    public string? BanReason { get; set; }

    // ── Profile mở rộng ─────────────────────────────────────────────────

    public DateOnly? DateOfBirth { get; set; }

    public Gender Gender { get; set; } = Gender.PreferNotToSay;

    /// <summary>Thành phố / khu vực.</summary>
    public string? City { get; set; }

    /// <summary>Giới thiệu ngắn về bản thân (tối đa 300 ký tự).</summary>
    public string? Bio { get; set; }

    /// <summary>Danh sách sở thích, phân tách bởi dấu phẩy. Ví dụ: "Nhạc,Phim,Du lịch"</summary>
    public string? Interests { get; set; }

    /// <summary>True nếu user đã điền đầy đủ thông tin Onboarding.</summary>
    public bool IsProfileComplete { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // ── Economy (Tiền tệ duy nhất) ──────────────────────────────────────
    public int Diamonds { get; set; }

    public DateOnly? LastDiamondRewardDate { get; set; }

    public DateOnly? LastCoPresenceCoinDate { get; set; }

    // ── Subscriptions ───────────────────────────────────────────────────
    public bool IsPremium { get; set; }
    public DateTimeOffset? PremiumUntil { get; set; }

    public bool IsGold { get; set; }
    public DateTimeOffset? GoldUntil { get; set; }
}
