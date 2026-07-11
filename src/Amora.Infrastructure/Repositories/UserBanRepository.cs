using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;
using Amora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Amora.Infrastructure.Repositories;

public sealed class UserBanRepository : IUserBanRepository
{
    private readonly AmoraDbContext _dbContext;

    public UserBanRepository(AmoraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserBan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserBans
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<UserBan?> GetActiveBanByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserBans
            .Where(x => x.UserId == userId && x.IsActive)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<UserBan> Items, int TotalCount)> GetPendingAppealsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.UserBans
            .Include(x => x.User)
            .Where(x => x.IsActive && x.AppealStatus == AppealStatus.Pending);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(UserBan userBan, CancellationToken cancellationToken = default)
    {
        _dbContext.UserBans.Add(userBan);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(UserBan userBan, CancellationToken cancellationToken = default)
    {
        _dbContext.UserBans.Update(userBan);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<int> CountPendingAppealsCountAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.UserBans.CountAsync(b => b.IsActive && b.AppealStatus == AppealStatus.Pending, cancellationToken);
    }

    public Task<int> CountAiBansAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.UserBans.CountAsync(b => b.BanReason.Contains("[AI AUTOMATED]"), cancellationToken);
    }
}
