using Amora.Domain.Enums;

namespace Amora.Domain.Entities;

public sealed class AppUser
{
    public Guid Id { get; set; }

    public string? Email { get; set; }

    public string? PasswordHash { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string AvatarUrl { get; set; } = string.Empty;

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

    // ── Pet Shop currency ───────────────────────────────────────────────
    public int PetCoins { get; set; }

    public int AmoraGems { get; set; }

    public DateOnly? LastPetCoinRewardDate { get; set; }

    public DateOnly? LastCoPresenceCoinDate { get; set; }
}
