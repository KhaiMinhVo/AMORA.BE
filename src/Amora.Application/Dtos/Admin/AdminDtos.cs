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
