using Amora.Domain.Entities;

namespace Amora.Domain.Interfaces;

public interface IAudioPlayLogRepository
{
    Task AddAsync(AudioPlayLog log, CancellationToken cancellationToken = default);
    Task<int> CountPlaysBetweenAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default);
    Task<Dictionary<DateOnly, int>> GetDailyPlayCountsAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default);
}
