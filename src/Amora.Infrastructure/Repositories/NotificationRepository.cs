using Amora.Domain.Entities;
using Amora.Domain.Interfaces;
using Amora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Amora.Infrastructure.Repositories;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly AmoraDbContext _db;

    public NotificationRepository(AmoraDbContext db) => _db = db;

    public async Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _db.Notifications.Where(x => x.UserId == userId);
        
        var total = await query.CountAsync(cancellationToken);
        
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
            
        return (items, total);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _db.Notifications
            .Where(x => x.UserId == userId && !x.IsRead)
            .CountAsync(cancellationToken);
    }

    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _db.Notifications.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task MarkAsReadAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var notif = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);
        if (notif != null && !notif.IsRead)
        {
            notif.IsRead = true;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _db.Notifications
            .Where(x => x.UserId == userId && !x.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), cancellationToken);
    }

    public async Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        await _db.Notifications
            .Where(x => x.Id == id && x.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        await _db.Notifications.AddAsync(notification, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
