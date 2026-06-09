using Amora.Domain.Entities;
using Amora.Domain.Interfaces;
using Amora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Amora.Infrastructure.Repositories;

public sealed class UserBlockRepository : IUserBlockRepository
{
    private readonly AmoraDbContext _dbContext;

    public UserBlockRepository(AmoraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(UserBlock block, CancellationToken cancellationToken = default)
    {
        await _dbContext.UserBlocks.AddAsync(block, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Guid blockerId, Guid blockedUserId, CancellationToken cancellationToken = default)
    {
        var block = await _dbContext.UserBlocks
            .FirstOrDefaultAsync(x => x.BlockerId == blockerId && x.BlockedUserId == blockedUserId, cancellationToken);

        if (block is not null)
        {
            _dbContext.UserBlocks.Remove(block);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public Task<bool> IsBlockedAsync(Guid blockerId, Guid blockedUserId, CancellationToken cancellationToken = default)
    {
        return _dbContext.UserBlocks.AnyAsync(
            x => x.BlockerId == blockerId && x.BlockedUserId == blockedUserId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetBlockedUserIdsAsync(Guid blockerId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserBlocks
            .AsNoTracking()
            .Where(x => x.BlockerId == blockerId)
            .Select(x => x.BlockedUserId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserBlock>> GetBlockedUsersAsync(Guid blockerId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserBlocks
            .AsNoTracking()
            .Where(x => x.BlockerId == blockerId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
