using Amora.Domain.Entities;
using Amora.Domain.Interfaces;
using Amora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Amora.Infrastructure.Repositories;

public sealed class AudioPlayLogRepository : IAudioPlayLogRepository
{
    private readonly AmoraDbContext _dbContext;

    public AudioPlayLogRepository(AmoraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(AudioPlayLog log, CancellationToken cancellationToken = default)
    {
        await _dbContext.AudioPlayLogs.AddAsync(log, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CountPlaysBetweenAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AudioPlayLogs
            .Where(x => x.PlayedAt >= start && x.PlayedAt <= end)
            .CountAsync(cancellationToken);
    }

    public async Task<Dictionary<DateOnly, int>> GetDailyPlayCountsAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default)
    {
        var logs = await _dbContext.AudioPlayLogs
            .Where(x => x.PlayedAt >= start && x.PlayedAt <= end)
            .GroupBy(x => x.PlayedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return logs.ToDictionary(x => DateOnly.FromDateTime(x.Date), x => x.Count);
    }
}
