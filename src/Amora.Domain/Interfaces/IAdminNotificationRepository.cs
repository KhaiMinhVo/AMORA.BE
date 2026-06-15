using Amora.Domain.Entities;

namespace Amora.Domain.Interfaces;

public interface IAdminNotificationRepository
{
    Task AddAsync(AdminNotification notification, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<AdminNotification> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default);
    Task<AdminNotification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(CancellationToken cancellationToken = default);
}
