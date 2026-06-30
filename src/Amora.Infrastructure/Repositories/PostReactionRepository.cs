using Amora.Application.Repositories;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Amora.Infrastructure.Repositories;

public sealed class PostReactionRepository : IPostReactionRepository
{
    private readonly AmoraDbContext _dbContext;

    public PostReactionRepository(AmoraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PostReaction?> GetReactionAsync(Guid postId, Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.PostReactions
            .FirstOrDefaultAsync(x => x.PostId == postId && x.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, ReactionType>> GetUserReactionsAsync(Guid userId, IEnumerable<Guid> postIds, CancellationToken cancellationToken = default)
    {
        var postIdsList = postIds.ToList();
        var reactions = await _dbContext.PostReactions
            .Where(x => x.UserId == userId && postIdsList.Contains(x.PostId))
            .ToDictionaryAsync(x => x.PostId, x => x.Type, cancellationToken);

        return reactions;
    }

    public async Task<(IReadOnlyList<PostReaction> Items, int TotalCount)> GetReactionsByPostIdAsync(Guid postId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.PostReactions
            .Include(x => x.User)
            .Where(x => x.PostId == postId)
            .OrderByDescending(x => x.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(PostReaction reaction, CancellationToken cancellationToken = default)
    {
        await _dbContext.PostReactions.AddAsync(reaction, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(PostReaction reaction, CancellationToken cancellationToken = default)
    {
        _dbContext.PostReactions.Remove(reaction);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PostReaction reaction, CancellationToken cancellationToken = default)
    {
        _dbContext.PostReactions.Update(reaction);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
