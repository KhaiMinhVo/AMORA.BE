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
        var viewer = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == viewerId, cancellationToken);
        if (viewer == null) return (Array.Empty<VoicePost>(), 0);

        // Lấy danh sách user bị block (subquery trong DB, không load vào RAM)
        var blockedUserIds = _dbContext.UserBlocks
            .Where(b => b.BlockerId == viewerId)
            .Select(b => b.BlockedUserId);

        // Lấy danh sách user đã match để phục vụ filter quyền riêng tư
        var matchedUserIds = _dbContext.MatchConnections
            .Where(m => (m.UserAId == viewerId || m.UserBId == viewerId) && m.Status == Amora.Domain.Enums.MatchStatus.Active)
            .Select(m => m.UserAId == viewerId ? m.UserBId : m.UserAId);

        var now = DateTimeOffset.UtcNow;
        var query = _dbContext.VoicePosts
            .AsNoTracking()
            .Join(_dbContext.Users, p => p.PosterId, u => u.Id, (p, u) => new { Post = p, Poster = u })
            .Where(x => x.Post.Status == Amora.Domain.Enums.VoicePostStatus.Open
                         && x.Post.PosterId != viewerId
                         && !blockedUserIds.Contains(x.Post.PosterId) // Ẩn post từ user bị block
                         && (x.Poster.VoicePrivacy == Amora.Domain.Enums.PrivacyLevel.Everyone 
                             || (x.Poster.VoicePrivacy == Amora.Domain.Enums.PrivacyLevel.MatchedOnly && matchedUserIds.Contains(x.Post.PosterId))))
            .Where(x => 
                 // Viewer's TargetGender matches Poster's Gender
                 (viewer.TargetGender == Amora.Domain.Enums.TargetGender.Both || 
                 (viewer.TargetGender == Amora.Domain.Enums.TargetGender.Male && x.Poster.Gender == Amora.Domain.Enums.Gender.Male) ||
                 (viewer.TargetGender == Amora.Domain.Enums.TargetGender.Female && x.Poster.Gender == Amora.Domain.Enums.Gender.Female))
                 &&
                 // Poster's TargetGender matches Viewer's Gender
                 (x.Poster.TargetGender == Amora.Domain.Enums.TargetGender.Both ||
                 (x.Poster.TargetGender == Amora.Domain.Enums.TargetGender.Male && viewer.Gender == Amora.Domain.Enums.Gender.Male) ||
                 (x.Poster.TargetGender == Amora.Domain.Enums.TargetGender.Female && viewer.Gender == Amora.Domain.Enums.Gender.Female))
            )
            .Select(x => new
            {
                Post = x.Post,
                IsBoosted = _dbContext.PostBoostRecords.Any(b => b.PostId == x.Post.Id && b.ExpiresAt > now),
                IsPreferredTone = viewer.PreferredVoiceTones != null && x.Post.Tone.HasValue && viewer.PreferredVoiceTones.Contains(x.Post.Tone.Value)
            })
            .OrderByDescending(x => x.IsBoosted)
            .ThenByDescending(x => (x.Post.ReactionCount * 2) + (x.Post.MatchCount * 5) + (x.IsPreferredTone ? 50 : 0))
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

    public async Task<Dictionary<DateOnly, int>> GetDailyCountsAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default)
    {
        var dates = await _dbContext.VoicePosts
            .Where(x => x.CreatedAt >= start && x.CreatedAt <= end)
            .Select(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return dates
            .GroupBy(x => DateOnly.FromDateTime(x.ToOffset(TimeSpan.FromHours(7)).DateTime))
            .ToDictionary(g => g.Key, g => g.Count());
    }
}