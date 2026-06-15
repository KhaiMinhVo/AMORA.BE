using Amora.Domain.Entities;
using Amora.Domain.Interfaces;
using Amora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Amora.Infrastructure.Repositories;

public sealed class AdminNotificationRepository : IAdminNotificationRepository
{
    private readonly AmoraDbContext _dbContext;

    public AdminNotificationRepository(AmoraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(AdminNotification notification, CancellationToken cancellationToken = default)
    {
        _dbContext.AdminNotifications.Add(notification);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<AdminNotification> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AdminNotifications.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.AdminNotifications
            .CountAsync(x => !x.IsRead, cancellationToken);
    }

    public async Task<AdminNotification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AdminNotifications.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _dbContext.AdminNotifications
            .Where(x => x.Id == id && !x.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRead, true), cancellationToken);
    }

    public async Task MarkAllAsReadAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.AdminNotifications
            .Where(x => !x.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRead, true), cancellationToken);
    }
}
