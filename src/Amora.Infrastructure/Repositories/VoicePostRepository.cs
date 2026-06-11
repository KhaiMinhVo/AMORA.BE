using Amora.Domain.Entities;
using Amora.Domain.Interfaces;
using Amora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Amora.Infrastructure.Repositories;

public sealed class VoicePostRepository : IVoicePostRepository
{
    private readonly AmoraDbContext _dbContext;

    public VoicePostRepository(AmoraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<VoicePost?> GetByIdAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        return _dbContext.VoicePosts.FirstOrDefaultAsync(x => x.Id == postId, cancellationToken);
    }

    public Task<int> CountByPosterSinceAsync(Guid posterId, DateTimeOffset since, CancellationToken cancellationToken = default)
    {
        return _dbContext.VoicePosts.CountAsync(x => x.PosterId == posterId && x.CreatedAt >= since, cancellationToken);
    }

    public async Task<(IReadOnlyList<VoicePost> Items, int TotalCount)> GetFeedPageAsync(
        Guid viewerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Lấy danh sách user bị block (subquery trong DB, không load vào RAM)
        var blockedUserIds = _dbContext.UserBlocks
            .Where(b => b.BlockerId == viewerId)
            .Select(b => b.BlockedUserId);

        var now = DateTimeOffset.UtcNow;
        var query = _dbContext.VoicePosts
            .AsNoTracking()
            .Where(x => x.Status == Amora.Domain.Enums.VoicePostStatus.Open
                         && x.PosterId != viewerId
                         && !blockedUserIds.Contains(x.PosterId)) // Ẩn post từ user bị block
            .Select(x => new
            {
                Post = x,
                IsBoosted = _dbContext.PostBoostRecords.Any(b => b.PostId == x.Id && b.ExpiresAt > now)
            })
            .OrderByDescending(x => x.IsBoosted)
            .ThenByDescending(x => x.Post.CreatedAt)
            .ThenByDescending(x => x.Post.Id);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var posts = items.Select(x =>
        {
            x.Post.IsBoosted = x.IsBoosted;
            return x.Post;
        }).ToList();

        return (posts, totalCount);
    }

    public async Task<(IReadOnlyList<VoicePost> Items, int TotalCount)> GetMyPostsPageAsync(
        Guid posterId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var query = _dbContext.VoicePosts
            .AsNoTracking()
            .Where(x => x.PosterId == posterId)
            .Select(x => new
            {
                Post = x,
                IsBoosted = _dbContext.PostBoostRecords.Any(b => b.PostId == x.Id && b.ExpiresAt > now)
            })
            .OrderByDescending(x => x.IsBoosted)
            .ThenByDescending(x => x.Post.CreatedAt)
            .ThenByDescending(x => x.Post.Id);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var posts = items.Select(x =>
        {
            x.Post.IsBoosted = x.IsBoosted;
            return x.Post;
        }).ToList();

        return (posts, totalCount);
    }

    public async Task AddAsync(VoicePost post, CancellationToken cancellationToken = default)
    {
        await _dbContext.VoicePosts.AddAsync(post, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(VoicePost post, CancellationToken cancellationToken = default)
    {
        if (_dbContext.Entry(post).State == EntityState.Detached)
        {
            _dbContext.VoicePosts.Update(post);
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}