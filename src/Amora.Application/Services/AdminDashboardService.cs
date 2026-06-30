using System.Diagnostics;
using Amora.Application.Abstractions;
using Amora.Application.Dtos.Admin;
using Amora.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Amora.Application.Services;

public sealed class AdminDashboardService
{
    private readonly IUserRepository _userRepository;
    private readonly IMatchConnectionRepository _matchRepository;
    private readonly IUserReportRepository _reportRepository;
    private readonly IVoicePostRepository _voicePostRepository;
    private readonly IVoiceCommentRepository _voiceCommentRepository;
    private readonly IAudioPlayLogRepository _audioPlayLogRepository;
    private readonly IStorageService _storageService;

    public AdminDashboardService(
        IUserRepository userRepository,
        IMatchConnectionRepository matchRepository,
        IUserReportRepository reportRepository,
        IVoicePostRepository voicePostRepository,
        IVoiceCommentRepository voiceCommentRepository,
        IAudioPlayLogRepository audioPlayLogRepository,
        IStorageService storageService)
    {
        _userRepository = userRepository;
        _matchRepository = matchRepository;
        _reportRepository = reportRepository;
        _voicePostRepository = voicePostRepository;
        _voiceCommentRepository = voiceCommentRepository;
        _audioPlayLogRepository = audioPlayLogRepository;
        _storageService = storageService;
    }

    public async Task<DashboardStatsResponseDto> GetDashboardStatsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);
        var sixtyDaysAgo = now.AddDays(-60);
        var sevenDaysAgo = now.AddDays(-6);

        var totalUsers = await _userRepository.CountUsersAsync(cancellationToken);
        var usersLast30Days = await _userRepository.CountUsersCreatedBetweenAsync(thirtyDaysAgo, now, cancellationToken);
        var usersPrev30Days = await _userRepository.CountUsersCreatedBetweenAsync(sixtyDaysAgo, thirtyDaysAgo, cancellationToken);
        var totalMatches = await _matchRepository.CountTotalMatchesAsync(cancellationToken);
        var pendingReports = await _reportRepository.CountPendingReportsAsync(cancellationToken);
        var latestUsersList = await _userRepository.GetLatestUsersAsync(5, cancellationToken);

        double growth = 0;
        if (usersPrev30Days == 0)
        {
            if (usersLast30Days > 0)
                growth = 100;
        }
        else
        {
            growth = ((double)(usersLast30Days - usersPrev30Days) / usersPrev30Days) * 100;
        }

        var latestUsersDto = latestUsersList.Select(u => new UserSummaryDto
        {
            UserId = u.Id,
            DisplayName = u.DisplayName,
            Email = u.Email ?? string.Empty,
            AvatarUrl = u.AvatarUrl,
            CreatedAt = u.CreatedAt,
            IsBanned = u.IsBanned
        }).ToList();

        // Voice Traffic Calculation
        var trafficData = new List<TrafficPointDto>();
        var postCounts = await _voicePostRepository.GetDailyCountsAsync(sevenDaysAgo, now, cancellationToken);
        var commentCounts = await _voiceCommentRepository.GetDailyCountsAsync(sevenDaysAgo, now, cancellationToken);
        var playCounts = await _audioPlayLogRepository.GetDailyPlayCountsAsync(sevenDaysAgo, now, cancellationToken);

        for (int i = 6; i >= 0; i--)
        {
            var date = DateOnly.FromDateTime(now.AddDays(-i).Date);
            var recordings = postCounts.GetValueOrDefault(date, 0) + commentCounts.GetValueOrDefault(date, 0);
            var plays = playCounts.GetValueOrDefault(date, 0);
            
            trafficData.Add(new TrafficPointDto
            {
                Day = date.ToString("ddd").ToUpperInvariant(),
                Recordings = recordings,
                Plays = plays
            });
        }

        // System Health Calculation
        var systemHealth = new SystemHealthDto();
        try
        {
            // Storage
            var totalBytes = await _storageService.GetTotalStorageSizeAsync(cancellationToken);
            var maxStorageBytes = 50L * 1024 * 1024 * 1024; // 50GB soft limit
            var storagePercentage = Math.Min(100, (double)totalBytes / maxStorageBytes * 100);

            // Backups
            var (backupSize, lastBackupTime) = await _storageService.GetLatestBackupInfoAsync(cancellationToken);
            string backupSizeStr = backupSize > 0 ? FormatBytes(backupSize) : "0B";

            // Uptime
            var process = Process.GetCurrentProcess();
            var uptime = DateTime.UtcNow - process.StartTime.ToUniversalTime();
            var uptimePercentage = 99.9; // Mock real uptime SLA percentage calculation or hardware stat.

            systemHealth = new SystemHealthDto
            {
                ApiLatencyMs = 45, // Mock value, you might need a proper middleware to measure this
                StorageUsedPercentage = Math.Round(storagePercentage, 2),
                UptimePercentage = uptimePercentage,
                BackupSize = backupSizeStr,
                LastBackupTime = lastBackupTime
            };
        }
        catch (Exception)
        {
            // Silent catch to prevent failing dashboard if S3 is down or misconfigured
        }

        return new DashboardStatsResponseDto
        {
            TotalUsers = totalUsers,
            UsersLast30Days = usersLast30Days,
            UsersPrevious30Days = usersPrev30Days,
            UserGrowthPercentage = Math.Round(growth, 2),
            TotalVoiceMatches = totalMatches,
            PendingReports = pendingReports,
            LatestUsers = latestUsersDto,
            TrafficData = trafficData,
            SystemHealth = systemHealth
        };
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffix = { "B", "KB", "MB", "GB", "TB" };
        int i = 0;
        double dblSByte = bytes;
        if (bytes > 1024)
            for (i = 0; (bytes / 1024) > 0 && i < suffix.Length - 1; i++, bytes /= 1024)
                dblSByte = bytes / 1024.0;

        return $"{Math.Round(dblSByte, 2)}{suffix[i]}";
    }
}
