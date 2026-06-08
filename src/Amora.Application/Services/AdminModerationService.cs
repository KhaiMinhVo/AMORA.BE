using Amora.Application.Abstractions;
using Amora.Application.Dtos.Admin;
using Amora.Application.Exceptions;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;

namespace Amora.Application.Services;

public sealed class AdminModerationService
{
    private readonly IUserReportRepository _reportRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUserBanRepository _userBanRepository;
    private readonly IRealtimeNotifier _realtimeNotifier;

    public AdminModerationService(
        IUserReportRepository reportRepository,
        IUserRepository userRepository,
        IUserBanRepository userBanRepository,
        IRealtimeNotifier realtimeNotifier)
    {
        _reportRepository = reportRepository;
        _userRepository = userRepository;
        _userBanRepository = userBanRepository;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<PaginatedList<ReportDto>> GetReportsAsync(int page, int pageSize, string? statusStr, CancellationToken cancellationToken = default)
    {
        ReportStatus? status = null;
        if (!string.IsNullOrEmpty(statusStr) && Enum.TryParse<ReportStatus>(statusStr, true, out var parsedStatus))
        {
            status = parsedStatus;
        }

        var (reports, totalCount) = await _reportRepository.GetReportsAsync(page, pageSize, status, cancellationToken);

        var reportDtos = new List<ReportDto>();
        foreach (var report in reports)
        {
            var targetUser = await _userRepository.GetByIdAsync(report.TargetUserId, cancellationToken);
            reportDtos.Add(new ReportDto
            {
                Id = report.Id,
                ReporterId = report.ReporterId,
                TargetUserId = report.TargetUserId,
                TargetDisplayName = targetUser?.DisplayName ?? "Unknown",
                TargetEmail = targetUser?.Email ?? "Unknown",
                Reason = report.Reason.ToString(),
                Description = report.Description,
                Status = report.Status.ToString(),
                CreatedAt = report.CreatedAt
            });
        }

        return new PaginatedList<ReportDto>
        {
            Items = reportDtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task ResolveReportAsync(Guid reportId, ResolveReportRequest request, CancellationToken cancellationToken = default)
    {
        var report = await _reportRepository.GetByIdAsync(reportId, cancellationToken)
            ?? throw new NotFoundApiException("Report not found.");

        if (report.Status != ReportStatus.Pending)
            throw new ValidationApiException("Report is already resolved.");

        if (request.Action.Equals("Ban", StringComparison.OrdinalIgnoreCase))
        {
            await BanUserAsync(report.TargetUserId, new BanUserRequest
            {
                Reason = request.ResolutionNote ?? "Banned due to user report.",
                DurationDays = request.BanDurationDays
            }, cancellationToken);

            report.Status = ReportStatus.ActionTaken;
        }
        else if (request.Action.Equals("Ignore", StringComparison.OrdinalIgnoreCase) || request.Action.Equals("Reject", StringComparison.OrdinalIgnoreCase))
        {
            report.Status = ReportStatus.Dismissed;
        }
        else if (request.Action.Equals("Warning", StringComparison.OrdinalIgnoreCase))
        {
            // Just resolve it and maybe send a warning via realtime notifier in future
            report.Status = ReportStatus.ActionTaken;
        }
        else
        {
            throw new ValidationApiException("Invalid action. Supported: Ban, Ignore, Warning.");
        }

        await _reportRepository.UpdateAsync(report, cancellationToken);
    }

    public async Task BanUserAsync(Guid userId, BanUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdForUpdateAsync(userId, cancellationToken)
            ?? throw new NotFoundApiException("User not found.");

        if (user.Role == "Admin")
            throw new ValidationApiException("Cannot ban an admin.");

        user.IsBanned = true;
        
        DateTimeOffset? bannedUntil = null;
        if (request.DurationDays.HasValue && request.DurationDays.Value > 0)
        {
            bannedUntil = DateTimeOffset.UtcNow.AddDays(request.DurationDays.Value);
        }

        var ban = new Amora.Domain.Entities.UserBan
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            BanReason = request.Reason ?? "Banned by Admin",
            BannedUntil = bannedUntil,
            CreatedAt = DateTimeOffset.UtcNow,
            IsActive = true
        };

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _userBanRepository.AddAsync(ban, cancellationToken);

        // Disconnect user from SignalR
        await _realtimeNotifier.DisconnectUserAsync(userId, request.Reason ?? "You have been banned.", cancellationToken);
    }

    public async Task UnbanUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdForUpdateAsync(userId, cancellationToken)
            ?? throw new NotFoundApiException("User not found.");

        user.IsBanned = false;
        await _userRepository.UpdateAsync(user, cancellationToken);

        var activeBan = await _userBanRepository.GetActiveBanByUserIdAsync(userId, cancellationToken);
        if (activeBan != null)
        {
            activeBan.IsActive = false;
            await _userBanRepository.UpdateAsync(activeBan, cancellationToken);
        }
    }

    public async Task<PaginatedList<AppealDto>> GetPendingAppealsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (bans, totalCount) = await _userBanRepository.GetPendingAppealsAsync(page, pageSize, cancellationToken);

        var dtos = bans.Select(b => new AppealDto
        {
            UserId = b.UserId,
            DisplayName = b.User?.DisplayName ?? "Unknown",
            Email = b.User?.Email ?? "",
            BanReason = b.BanReason,
            BannedUntil = b.BannedUntil,
            AppealReason = b.AppealReason
        }).ToList();

        return new PaginatedList<AppealDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task ResolveAppealAsync(Guid userId, ResolveAppealRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdForUpdateAsync(userId, cancellationToken)
            ?? throw new NotFoundApiException("User not found.");

        var activeBan = await _userBanRepository.GetActiveBanByUserIdAsync(userId, cancellationToken)
            ?? throw new NotFoundApiException("Active ban not found.");

        if (activeBan.AppealStatus != AppealStatus.Pending)
            throw new ValidationApiException("User does not have a pending appeal.");

        if (request.Action.Equals("Approve", StringComparison.OrdinalIgnoreCase))
        {
            user.IsBanned = false;
            activeBan.IsActive = false;
            activeBan.AppealStatus = AppealStatus.Approved;
        }
        else if (request.Action.Equals("Reject", StringComparison.OrdinalIgnoreCase))
        {
            activeBan.AppealStatus = AppealStatus.Rejected;
        }
        else
        {
            throw new ValidationApiException("Invalid action. Supported: Approve, Reject.");
        }

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _userBanRepository.UpdateAsync(activeBan, cancellationToken);
    }
}
