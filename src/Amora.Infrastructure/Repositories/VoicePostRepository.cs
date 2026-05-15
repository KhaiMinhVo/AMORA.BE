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
        return _dbContext.VoicePosts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == postId, cancellationToken);
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
        var query = _dbContext.VoicePosts
            .AsNoTracking()
            .Where(x => x.Status == Amora.Domain.Enums.VoicePostStatus.Open && x.PosterId != viewerId)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(VoicePost post, CancellationToken cancellationToken = default)
    {
        await _dbContext.VoicePosts.AddAsync(post, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(VoicePost post, CancellationToken cancellationToken = default)
    {
        _dbContext.VoicePosts.Update(post);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}