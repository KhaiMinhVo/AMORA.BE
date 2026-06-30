using Amora.Domain.Entities;
using Amora.Domain.Enums;

namespace Amora.Application.Repositories;

public interface IPostReactionRepository
{
    Task<PostReaction?> GetReactionAsync(Guid postId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, ReactionType>> GetUserReactionsAsync(Guid userId, IEnumerable<Guid> postIds, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<PostReaction> Items, int TotalCount)> GetReactionsByPostIdAsync(Guid postId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task AddAsync(PostReaction reaction, CancellationToken cancellationToken = default);
    Task DeleteAsync(PostReaction reaction, CancellationToken cancellationToken = default);
    Task UpdateAsync(PostReaction reaction, CancellationToken cancellationToken = default);
}
