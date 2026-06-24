namespace Amora.Application.Dtos.Admin;

public sealed class UserSummaryDto
{
    public Guid UserId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string AvatarUrl { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class DashboardStatsResponseDto
{
    public int TotalUsers { get; init; }
    
    // Growth metrics based on 30-day sliding window
    public int UsersLast30Days { get; init; }
    public int UsersPrevious30Days { get; init; }
    public double UserGrowthPercentage { get; init; }
    
    public int TotalVoiceMatches { get; init; }
    public int PendingReports { get; init; }
    public List<UserSummaryDto> LatestUsers { get; init; } = [];
}
