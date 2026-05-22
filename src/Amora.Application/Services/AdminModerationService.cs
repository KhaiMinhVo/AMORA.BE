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
    private readonly IRealtimeNotifier _realtimeNotifier;

    public AdminModerationService(
        IUserReportRepository reportRepository,
        IUserRepository userRepository,
        IRealtimeNotifier realtimeNotifier)
    {
        _reportRepository = reportRepository;
        _userRepository = userRepository;
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
        user.BanReason = request.Reason;
        
        if (request.DurationDays.HasValue && request.DurationDays.Value > 0)
        {
            user.BannedUntil = DateTimeOffset.UtcNow.AddDays(request.DurationDays.Value);
        }
        else
        {
            user.BannedUntil = null; // Permanent ban
        }

        await _userRepository.UpdateAsync(user, cancellationToken);

        // Disconnect user from SignalR
        await _realtimeNotifier.DisconnectUserAsync(userId, request.Reason ?? "You have been banned.", cancellationToken);
    }

    public async Task UnbanUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdForUpdateAsync(userId, cancellationToken)
            ?? throw new NotFoundApiException("User not found.");

        user.IsBanned = false;
        user.BannedUntil = null;
        user.BanReason = null;

        await _userRepository.UpdateAsync(user, cancellationToken);
    }
}
