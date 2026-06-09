namespace Amora.Application.Dtos.Safety;

public sealed class CreateReportRequest
{
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? TargetPostId { get; set; }
    public Guid? TargetCommentId { get; set; }
}

public sealed class ReportResponseDto
{
    public Guid ReportId { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class BlockResponseDto
{
    public Guid BlockedUserId { get; init; }
    public string Status { get; init; } = string.Empty;
}

public sealed class BlockedUserDto
{
    public Guid UserId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string AvatarUrl { get; init; } = string.Empty;
    public DateTimeOffset BlockedAt { get; init; }
}
