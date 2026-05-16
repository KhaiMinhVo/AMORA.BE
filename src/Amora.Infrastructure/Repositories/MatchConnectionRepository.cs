using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;
using Amora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Amora.Infrastructure.Repositories;

public sealed class MatchConnectionRepository : IMatchConnectionRepository
{
    private readonly AmoraDbContext _dbContext;

    public MatchConnectionRepository(AmoraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(MatchConnection MatchConnection, bool PostClosed)> CreateConnectionAsync(
        Guid postId,
        Guid commentId,
        Guid posterId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var post = await _dbContext.VoicePosts
            .FromSqlInterpolated($"SELECT * FROM \"VoicePosts\" WHERE \"Id\" = {postId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

        if (post is null)
        {
            throw new InvalidOperationException("Voice post not found.");
        }

        if (post.PosterId != posterId)
        {
            throw new InvalidOperationException("You cannot match on this post.");
        }

        if (post.Status == VoicePostStatus.Closed)
        {
            throw new InvalidOperationException("This post is already closed.");
        }

        var comment = await _dbContext.VoiceComments.SingleOrDefaultAsync(x => x.Id == commentId, cancellationToken);
        if (comment is null)
        {
            throw new InvalidOperationException("Voice comment not found.");
        }

        if (comment.PostId != postId)
        {
            throw new InvalidOperationException("Comment does not belong to the selected post.");
        }

        if (comment.Status != VoiceCommentStatus.Pending)
        {
            throw new InvalidOperationException("Comment has already been processed.");
        }

        comment.Status = VoiceCommentStatus.Accepted;
        post.MatchCount += 1;

        var postClosed = post.MatchCount >= 3;
        if (postClosed)
        {
            post.Status = VoicePostStatus.Closed;
        }

        var matchConnection = new MatchConnection
        {
            Id = Guid.NewGuid(),
            PostId = post.Id,
            UserAId = post.PosterId,
            UserBId = comment.CommenterId,
            Status = MatchStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24)
        };

        _dbContext.MatchConnections.Add(matchConnection);
        _dbContext.VoiceComments.Update(comment);
        _dbContext.VoicePosts.Update(post);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return (matchConnection, postClosed);
    }

    public async Task<IReadOnlyList<MatchConnection>> GetActiveByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MatchConnections
            .AsNoTracking()
            .Where(x => x.Status == MatchStatus.Active && (x.UserAId == userId || x.UserBId == userId))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<MatchConnection?> GetByIdAsync(Guid matchId, CancellationToken cancellationToken = default)
    {
        return _dbContext.MatchConnections.AsNoTracking().FirstOrDefaultAsync(x => x.Id == matchId, cancellationToken);
    }

    public Task<bool> IsParticipantAsync(Guid matchId, Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.MatchConnections.AnyAsync(x => x.Id == matchId && (x.UserAId == userId || x.UserBId == userId), cancellationToken);
    }

    public Task<bool> AreMatchedAsync(Guid userAId, Guid userBId, CancellationToken cancellationToken = default)
    {
        return _dbContext.MatchConnections.AnyAsync(
            x => x.Status == MatchStatus.Active
                 && ((x.UserAId == userAId && x.UserBId == userBId)
                     || (x.UserAId == userBId && x.UserBId == userAId)),
            cancellationToken);
    }

    public async Task<IReadOnlyList<MatchConnection>> GetExpiredMatchesAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        return await _dbContext.MatchConnections
            .Where(x => x.Status == MatchStatus.Active && x.ExpiresAt <= now)
            .OrderBy(x => x.ExpiresAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> ExpireMatchesAsync(IReadOnlyList<Guid> matchIds, CancellationToken cancellationToken = default)
    {
        if (matchIds.Count == 0) return 0;

        return await _dbContext.MatchConnections
            .Where(x => matchIds.Contains(x.Id) && x.Status == MatchStatus.Active)
            .ExecuteUpdateAsync(
                setter => setter.SetProperty(x => x.Status, MatchStatus.Expired),
                cancellationToken);
    }

    public async Task ExtendHandshakeAsync(Guid matchId, CancellationToken cancellationToken = default)
    {
        var newExpiry = DateTimeOffset.UtcNow.AddHours(24);

        await _dbContext.MatchConnections
            .Where(x => x.Id == matchId && x.Status == MatchStatus.Active)
            .ExecuteUpdateAsync(
                setter => setter.SetProperty(x => x.ExpiresAt, newExpiry),
                cancellationToken);
    }
}