using Amora.Domain.Entities;

namespace Amora.Domain.Interfaces;

public interface INotificationRepository
{
    Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
