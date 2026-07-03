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
        var dates = await _dbContext.AudioPlayLogs
            .Where(x => x.PlayedAt >= start && x.PlayedAt <= end)
            .Select(x => x.PlayedAt)
            .ToListAsync(cancellationToken);

        return dates
            .GroupBy(x => DateOnly.FromDateTime(x.ToOffset(TimeSpan.FromHours(7)).DateTime))
            .ToDictionary(g => g.Key, g => g.Count());
    }
}
