namespace Amora.Application.Dtos.Admin;

public sealed class UserSummaryDto
{
    public Guid UserId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string AvatarUrl { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? LastActiveAt { get; init; }
    public bool IsBanned { get; init; }
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
    public List<TrafficPointDto> TrafficData { get; init; } = [];
    public SystemHealthDto SystemHealth { get; init; } = new();
}

public sealed class TrafficPointDto
{
    public string Day { get; init; } = string.Empty; // e.g. "MON", "TUE"
    public int Recordings { get; init; }
    public int Plays { get; init; }
}

public sealed class SystemHealthDto
{
    public int ApiLatencyMs { get; init; }
    public double StorageUsedPercentage { get; init; }
    public double UptimePercentage { get; init; }
    public string BackupSize { get; init; } = "0B";
    public DateTimeOffset? LastBackupTime { get; init; }
}
