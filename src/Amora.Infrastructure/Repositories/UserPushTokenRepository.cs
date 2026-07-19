using Amora.Domain.Entities;
using Amora.Domain.Interfaces;
using Amora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Amora.Infrastructure.Repositories;

public sealed class UserPushTokenRepository : IUserPushTokenRepository
{
    private readonly AmoraDbContext _db;

    public UserPushTokenRepository(AmoraDbContext db) => _db = db;

    public async Task AddOrUpdateAsync(UserPushToken token, CancellationToken cancellationToken = default)
    {
        var existing = await _db.UserPushTokens
            .FirstOrDefaultAsync(x => x.UserId == token.UserId && x.DeviceId == token.DeviceId, cancellationToken);

        if (existing is not null)
        {
            existing.Token = token.Token;
            existing.Platform = token.Platform;
            existing.LastActiveAt = DateTimeOffset.UtcNow;
        }
        else
        {
            token.Id = Guid.NewGuid();
            token.CreatedAt = DateTimeOffset.UtcNow;
            token.LastActiveAt = DateTimeOffset.UtcNow;
            await _db.UserPushTokens.AddAsync(token, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveByDeviceAsync(Guid userId, string deviceId, CancellationToken cancellationToken = default)
    {
        await _db.UserPushTokens
            .Where(x => x.UserId == userId && x.DeviceId == deviceId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task RemoveByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        await _db.UserPushTokens
            .Where(x => x.Token == token)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserPushToken>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _db.UserPushTokens
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);
    }
}
