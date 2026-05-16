using Amora.Domain.Entities;

namespace Amora.Domain.Interfaces;

public interface IChatReadStateRepository
{
    Task<ChatReadState?> GetAsync(Guid userId, Guid matchId, CancellationToken cancellationToken = default);

    Task UpsertReadAsync(Guid userId, Guid matchId, DateTimeOffset readAt, CancellationToken cancellationToken = default);

    Task<int> CountUnreadAsync(Guid userId, Guid matchId, DateTimeOffset? since, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, int>> CountUnreadByMatchesAsync(Guid userId, IReadOnlyList<Guid> matchIds, CancellationToken cancellationToken = default);
}
