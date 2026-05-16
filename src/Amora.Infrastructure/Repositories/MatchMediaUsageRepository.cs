using Amora.Domain.Entities;
using Amora.Domain.Interfaces;
using Amora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Amora.Infrastructure.Repositories;

public sealed class MatchMediaUsageRepository : IMatchMediaUsageRepository
{
    private readonly AmoraDbContext _db;

    public MatchMediaUsageRepository(AmoraDbContext db) => _db = db;

    public Task<MatchDailyMediaUsage?> GetTodayAsync(Guid matchId, Guid userId, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return _db.MatchDailyMediaUsages.FirstOrDefaultAsync(
            x => x.MatchId == matchId && x.UserId == userId && x.UsageDate == today,
            cancellationToken);
    }

    public async Task IncrementImageSentAsync(Guid matchId, Guid userId, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var row = await GetTodayAsync(matchId, userId, cancellationToken);

        if (row is null)
        {
            await _db.MatchDailyMediaUsages.AddAsync(new MatchDailyMediaUsage
            {
                Id = Guid.NewGuid(),
                MatchId = matchId,
                UserId = userId,
                UsageDate = today,
                ImagesSent = 1,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }, cancellationToken);
        }
        else
        {
            row.ImagesSent++;
            row.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
