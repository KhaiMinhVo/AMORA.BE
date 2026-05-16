using Amora.Domain.Entities;

namespace Amora.Domain.Interfaces;

public interface IMatchMediaUsageRepository
{
    Task<MatchDailyMediaUsage?> GetTodayAsync(Guid matchId, Guid userId, CancellationToken cancellationToken = default);

    Task IncrementImageSentAsync(Guid matchId, Guid userId, CancellationToken cancellationToken = default);
}
