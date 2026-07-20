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
    private readonly IPetTransactionRepository _petTransactionRepository;
    private readonly IVoicePostRepository _voicePostRepository;
    private readonly NotificationService _notificationService;

    public AdminModerationService(
        IUserReportRepository reportRepository,
        IUserRepository userRepository,
        IUserBanRepository userBanRepository,
        IRealtimeNotifier realtimeNotifier,
        IPetTransactionRepository petTransactionRepository,
        IVoicePostRepository voicePostRepository,
        NotificationService notificationService)
    {
        _reportRepository = reportRepository;
        _userRepository = userRepository;
        _userBanRepository = userBanRepository;
        _realtimeNotifier = realtimeNotifier;
        _petTransactionRepository = petTransactionRepository;
        _voicePostRepository = voicePostRepository;
        _notificationService = notificationService;
    }

    public async Task<PaginatedList<AdminUserDto>> GetUsersAsync(int page, int pageSize, string? keyword, string? subscriptionType, bool? isBanned, CancellationToken cancellationToken = default)
    {
        var (users, totalCount) = await _userRepository.GetAllUsersAsync(page, pageSize, keyword, subscriptionType, isBanned, cancellationToken);
        var dtos = users.Select(u => new AdminUserDto
        {
            Id = u.Id,
            Email = u.Email ?? "",
            DisplayName = u.DisplayName,
            Role = u.Role,
            SubscriptionType = u.SubscriptionType.ToString(),
            IsBanned = u.IsBanned,
            CreatedAt = u.CreatedAt,
            AvatarUrl = u.AvatarUrl ?? "",
            LastActiveAt = u.LastActiveAt
        });

        return new PaginatedList<AdminUserDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task CreateAdminAsync(CreateAdminRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ValidationApiException("Email và mật khẩu không được để trống.");
        }

        var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser is not null)
        {
            throw new ValidationApiException("Email này đã được sử dụng.");
        }

        var newAdmin = new Amora.Domain.Entities.AppUser
        {
            Id = Guid.NewGuid(),
            Email = request.Email.Trim().ToLowerInvariant(),
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? "Admin" : request.DisplayName.Trim(),
            PasswordHash = Amora.Application.Services.PasswordHasher.Hash(request.Password),
            Role = "Admin",
            SubscriptionType = SubscriptionType.Free,
            CreatedAt = DateTimeOffset.UtcNow,
            LastActiveAt = DateTimeOffset.UtcNow
        };

        await _userRepository.AddAsync(newAdmin, cancellationToken);
    }

    public async Task<AdminUserDetailDto> GetUserDetailsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundApiException("Không tìm thấy người dùng.");

        var totalVoicePosts = await _voicePostRepository.CountByPosterSinceAsync(userId, DateTimeOffset.MinValue, cancellationToken);
        var totalReports = await _reportRepository.CountReportsAgainstUserAsync(userId, cancellationToken);

        return new AdminUserDetailDto
        {
            Id = user.Id,
            Email = user.Email ?? "",
            DisplayName = user.DisplayName,
            SubscriptionType = user.SubscriptionType.ToString(),
            Status = user.IsBanned ? "Banned" : "Active",
            LastActiveAt = user.LastActiveAt,
            TotalVoicePosts = totalVoicePosts,
            TotalReportsAgainstUser = totalReports,
            Diamonds = user.Diamonds
        };
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
            ?? throw new NotFoundApiException("Báo cáo không tồn tại.");

        if (report.Status != ReportStatus.Pending)
            throw new ValidationApiException("Báo cáo này đã được giải quyết.");

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
            throw new ValidationApiException("Hành động không hợp lệ. Chỉ hỗ trợ: Ban, Ignore, Warning.");
        }

        await _reportRepository.UpdateAsync(report, cancellationToken);

        // Notify the reporter
        var bodyMsg = !string.IsNullOrWhiteSpace(request.ResolutionNote) 
            ? $"Phản hồi từ quản trị viên: {request.ResolutionNote}" 
            : "Báo cáo của bạn đã được quản trị viên xử lý.";

        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            reportId = report.Id.ToString(),
            action = request.Action,
            resolutionNote = request.ResolutionNote
        });

        await _notificationService.SendNotificationAsync(
            report.ReporterId,
            NotificationType.System,
            "Báo cáo vi phạm đã được xử lý",
            bodyMsg,
            payload,
            cancellationToken);
    }

    public async Task RefundDiamondsAsync(Guid userId, int amount, string reason, CancellationToken cancellationToken = default)
    {
        if (amount == 0)
        {
            throw new ValidationApiException("Số lượng kim cương điều chỉnh phải khác 0.");
        }

        var user = await _userRepository.GetByIdForUpdateAsync(userId, cancellationToken)
            ?? throw new NotFoundApiException("Không tìm thấy người dùng.");

        if (user.Diamonds + amount < 0)
        {
            throw new ValidationApiException($"Không thể trừ {Math.Abs(amount)} kim cương. Số dư hiện tại ({user.Diamonds}) không đủ.");
        }

        user.Diamonds += amount;
        await _userRepository.UpdateAsync(user, cancellationToken);
        
        await _realtimeNotifier.NotifyDiamondBalanceChangedAsync(
            user.Id, 
            user.Diamonds, 
            amount, 
            $"Admin Update: {reason}", 
            cancellationToken);

        await _realtimeNotifier.NotifyAdminAsync(
            $"Admin vừa cộng/trừ {amount} Kim Cương cho User {user.DisplayName ?? user.Id.ToString()}. Lý do: {reason}",
            cancellationToken);

        var transaction = new Amora.Domain.Entities.PetTransaction
        {
            UserId = userId,
            TransactionType = $"AdminRefund: {reason}",
            DiamondsDelta = amount,
            CreatedAt = DateTime.UtcNow
        };
        await _petTransactionRepository.AddAsync(transaction, cancellationToken);
    }

    public async Task BanUserAsync(Guid userId, BanUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdForUpdateAsync(userId, cancellationToken)
            ?? throw new NotFoundApiException("Không tìm thấy người dùng.");

        if (user.Role == "Admin")
            throw new ValidationApiException("Không thể khóa tài khoản Admin.");

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
            ?? throw new NotFoundApiException("Không tìm thấy người dùng.");

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
            ?? throw new NotFoundApiException("Không tìm thấy người dùng.");

        var activeBan = await _userBanRepository.GetActiveBanByUserIdAsync(userId, cancellationToken)
            ?? throw new NotFoundApiException("Không tìm thấy lệnh cấm nào.");

        if (activeBan.AppealStatus != AppealStatus.Pending)
            throw new ValidationApiException("Người dùng không có đơn kháng cáo nào đang chờ xử lý.");

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
            throw new ValidationApiException("Hành động không hợp lệ. Chỉ hỗ trợ: Approve, Reject.");
        }

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _userBanRepository.UpdateAsync(activeBan, cancellationToken);
    }

    public async Task<ContentModerationStatsDto> GetModerationStatsAsync(CancellationToken cancellationToken = default)
    {
        var totalReports = await _reportRepository.CountAllReportsAsync(cancellationToken);
        var pendingReports = await _reportRepository.CountPendingReportsAsync(cancellationToken);
        var pendingAppeals = await _userBanRepository.CountPendingAppealsCountAsync(cancellationToken);
        var autoBlocked = await _userBanRepository.CountAllAiBansCountAsync(cancellationToken);
        
        return new ContentModerationStatsDto
        {
            TotalViolationReports = totalReports,
            PendingReview = pendingReports + pendingAppeals,
            AutoBlockedCount = autoBlocked
        };
    }
}
