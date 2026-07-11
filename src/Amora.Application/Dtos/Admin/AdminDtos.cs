using Amora.Domain.Enums;

namespace Amora.Application.Dtos.Admin;

public sealed class PaginatedList<T>
{
    public IEnumerable<T> Items { get; init; } = Array.Empty<T>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

public sealed class ReportDto
{
    public Guid Id { get; init; }
    public Guid ReporterId { get; init; }
    public Guid TargetUserId { get; init; }
    public string TargetDisplayName { get; init; } = string.Empty;
    public string TargetEmail { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class ResolveReportRequest
{
    /// <summary>Ignore, Warning, or Ban</summary>
    public string Action { get; init; } = string.Empty;

    public string? ResolutionNote { get; init; }

    /// <summary>Used if Action == "Ban". Days to ban, or null for permanent.</summary>
    public int? BanDurationDays { get; init; }
}

public sealed class BanUserRequest
{
    public string? Reason { get; init; }
    public int? DurationDays { get; init; }
}

public sealed class AppealDto
{
    public Guid UserId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? BanReason { get; init; }
    public DateTimeOffset? BannedUntil { get; init; }
    public string? AppealReason { get; init; }
}

public sealed class ResolveAppealRequest
{
    /// <summary>Approve (Unban) or Reject (Keep ban)</summary>
    public string Action { get; init; } = string.Empty;
}

public sealed class CreateAdminRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
}

public sealed class AdminUserDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string SubscriptionType { get; init; } = string.Empty;
    public bool IsBanned { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string AvatarUrl { get; init; } = string.Empty;
    public DateTimeOffset? LastActiveAt { get; init; }
}

public sealed class AdminUserDetailDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string SubscriptionType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty; // "Active" | "Banned"
    public DateTimeOffset? LastActiveAt { get; init; }
    public int TotalVoicePosts { get; init; }
    public int TotalReportsAgainstUser { get; init; }
    public int Diamonds { get; init; }
}

public sealed class ContentModerationStatsDto
{
    public int TotalViolationReports { get; set; }
    public int PendingReview { get; set; }
    public int AutoBlockedCount { get; set; }
}
