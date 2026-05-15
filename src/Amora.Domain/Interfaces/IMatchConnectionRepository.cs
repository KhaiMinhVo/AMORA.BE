using Amora.Domain.Entities;

namespace Amora.Domain.Interfaces;

public interface IMatchConnectionRepository
{
    Task<(MatchConnection MatchConnection, bool PostClosed)> CreateConnectionAsync(
        Guid postId,
        Guid commentId,
        Guid posterId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MatchConnection>> GetActiveByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<MatchConnection?> GetByIdAsync(Guid matchId, CancellationToken cancellationToken = default);

    Task<bool> IsParticipantAsync(Guid matchId, Guid userId, CancellationToken cancellationToken = default);
}