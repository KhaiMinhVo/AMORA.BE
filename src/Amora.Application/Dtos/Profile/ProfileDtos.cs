namespace Amora.Application.Dtos.Profile;

public sealed class UpdateProfileRequest
{
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string[]? Photos { get; set; }
    public string? DateOfBirth { get; set; }  // Format: "yyyy-MM-dd"
    public string? Gender { get; set; }       // "Male", "Female", "NonBinary", "Other", "PreferNotToSay"
    public string? TargetGender { get; set; } // "Male", "Female", "Both"
    public string? City { get; set; }
    public string? Bio { get; set; }
    public string? VoiceIntroUrl { get; set; }
    public int? VoiceIntroDuration { get; set; }
    public string? VoicePrivacy { get; set; }
    public string[]? Interests { get; set; }
}

public sealed class ProfileResponseDto
{
    public Guid UserId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string AvatarUrl { get; init; } = string.Empty;
    public string[] Photos { get; init; } = [];
    public string? DateOfBirth { get; init; }
    public string Gender { get; init; } = string.Empty;
    public string TargetGender { get; init; } = string.Empty;
    public string? City { get; init; }
    public string? Bio { get; init; }
    public string? VoiceIntroUrl { get; init; }
    public int? VoiceIntroDuration { get; init; }
    public string VoicePrivacy { get; init; } = "Everyone";
    public string[] Interests { get; init; } = [];
    public bool IsProfileComplete { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public int Diamonds { get; init; }
    public string SubscriptionType { get; init; } = "Free";
    public DateTimeOffset? SubscriptionEndDate { get; init; }
    public int TrustScore { get; init; }
    public bool AutoRenewEnabled { get; init; }
}

/// <summary>
/// Profile công khai — hiển thị cho người khác xem.
/// Avatar và Photos bị ẩn (trả về rỗng hoặc ảnh mặc định) nếu chưa match.
/// </summary>
public sealed class PublicProfileResponseDto
{
    public Guid UserId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string AvatarUrl { get; init; } = string.Empty;
    public string[] Photos { get; init; } = [];
    public string Gender { get; init; } = string.Empty;
    public string? City { get; init; }
    public string? Bio { get; init; }
    public string? VoiceIntroUrl { get; init; }
    public int? VoiceIntroDuration { get; init; }
    public string[] Interests { get; init; } = [];
    public int TrustScore { get; init; }
}
