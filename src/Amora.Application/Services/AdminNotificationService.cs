using Amora.Application.Abstractions;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;

namespace Amora.Application.Services;

public sealed class AdminNotificationService
{
    private readonly IAdminNotificationRepository _notificationRepository;
    private readonly IAdminNotifier _adminNotifier;

    public AdminNotificationService(
        IAdminNotificationRepository notificationRepository,
        IAdminNotifier adminNotifier)
    {
        _notificationRepository = notificationRepository;
        _adminNotifier = adminNotifier;
    }

    public async Task NotifyNewReportAsync(Guid reporterId, string reporterName, Guid targetUserId, string targetName, string targetType, string reason, CancellationToken cancellationToken = default)
    {
        var notification = new AdminNotification
        {
            Id = Guid.NewGuid(),
            Type = AdminNotificationType.NewReport,
            Title = $"Báo cáo vi phạm mới ({targetType})",
            Message = $"Người dùng {reporterName} đã báo cáo {targetName} với lý do: {reason}.",
            ActionUrl = "/admin/reports",
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _notificationRepository.AddAsync(notification, cancellationToken);
        await _adminNotifier.NotifyNewAdminAlertAsync(notification, cancellationToken);
    }

    public async Task NotifyNewAppealAsync(Guid userId, string reason, CancellationToken cancellationToken = default)
    {
        var notification = new AdminNotification
        {
            Id = Guid.NewGuid(),
            Type = AdminNotificationType.NewAppeal,
            Title = "Đơn khiếu nại mới",
            Message = $"Người dùng {userId.ToString()[..8]} đã gửi đơn khiếu nại xin mở khóa tài khoản. Lý do: {reason}",
            ActionUrl = "/admin/appeals",
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _notificationRepository.AddAsync(notification, cancellationToken);
        await _adminNotifier.NotifyNewAdminAlertAsync(notification, cancellationToken);
    }

    public async Task NotifyAutoBlockedContentAsync(string contentType, string reason, Guid userId, CancellationToken cancellationToken = default)
    {
        var notification = new AdminNotification
        {
            Id = Guid.NewGuid(),
            Type = AdminNotificationType.AutoBlockedContent,
            Title = "AI Tự động chặn nội dung",
            Message = $"Hệ thống AI vừa tự động chặn 1 {contentType} của người dùng {userId.ToString()[..8]} vì lý do: {reason}",
            ActionUrl = "/admin/users", // can point to user details or specific log
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _notificationRepository.AddAsync(notification, cancellationToken);
        await _adminNotifier.NotifyNewAdminAlertAsync(notification, cancellationToken);
    }

    public async Task NotifySystemAlertAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        var notification = new AdminNotification
        {
            Id = Guid.NewGuid(),
            Type = AdminNotificationType.SystemAlert,
            Title = title,
            Message = message,
            ActionUrl = "/admin/dashboard",
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _notificationRepository.AddAsync(notification, cancellationToken);
        await _adminNotifier.NotifyNewAdminAlertAsync(notification, cancellationToken);
    }
}
