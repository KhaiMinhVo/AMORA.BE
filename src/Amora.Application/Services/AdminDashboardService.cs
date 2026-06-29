using Amora.Application.Dtos.Admin;
using Amora.Domain.Interfaces;

namespace Amora.Application.Services;

public sealed class AdminDashboardService
{
    private readonly IUserRepository _userRepository;
    private readonly IMatchConnectionRepository _matchRepository;
    private readonly IUserReportRepository _reportRepository;

    public AdminDashboardService(
        IUserRepository userRepository,
        IMatchConnectionRepository matchRepository,
        IUserReportRepository reportRepository)
    {
        _userRepository = userRepository;
        _matchRepository = matchRepository;
        _reportRepository = reportRepository;
    }

    public async Task<DashboardStatsResponseDto> GetDashboardStatsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);
        var sixtyDaysAgo = now.AddDays(-60);

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
                growth = 100; // 100% growth if from 0 to something
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

        return new DashboardStatsResponseDto
        {
            TotalUsers = totalUsers,
            UsersLast30Days = usersLast30Days,
            UsersPrevious30Days = usersPrev30Days,
            UserGrowthPercentage = Math.Round(growth, 2),
            TotalVoiceMatches = totalMatches,
            PendingReports = pendingReports,
            LatestUsers = latestUsersDto
        };
    }
}
