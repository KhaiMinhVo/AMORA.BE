using Amora.Domain.Entities;

namespace Amora.Domain.Interfaces;

public interface IChatMessageRepository
{
    Task AddAsync(ChatMessage message, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<ChatMessage> Items, string? NextCursor)> GetByMatchAsync(
        Guid matchId,
        string? cursor,
        int limit,
        CancellationToken cancellationToken = default);
}