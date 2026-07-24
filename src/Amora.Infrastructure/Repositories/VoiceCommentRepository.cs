using Amora.Domain.Entities;
using Amora.Domain.Interfaces;
using Amora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Amora.Infrastructure.Repositories;

public sealed class VoiceCommentRepository : IVoiceCommentRepository
{
    private readonly AmoraDbContext _dbContext;

    public VoiceCommentRepository(AmoraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(VoiceComment comment, CancellationToken cancellationToken = default)
    {
        await _dbContext.VoiceComments.AddAsync(comment, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<VoiceComment?> GetByIdAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        return _dbContext.VoiceComments.FirstOrDefaultAsync(x => x.Id == commentId, cancellationToken);
    }

    public Task<bool> HasUserCommentedOnPostAsync(Guid userId, Guid postId, CancellationToken cancellationToken = default)
    {
        return _dbContext.VoiceComments.AnyAsync(x => x.CommenterId == userId && x.PostId == postId, cancellationToken);
    }

    public async Task<(IReadOnlyList<VoiceComment> Items, int TotalCount)> GetPagedByPostIdAsync(
        Guid postId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.VoiceComments
            .AsNoTracking()
            .Where(x => x.PostId == postId && x.Status == Domain.Enums.VoiceCommentStatus.Accepted)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public async Task UpdateAsync(VoiceComment comment, CancellationToken cancellationToken = default)
    {
        _dbContext.VoiceComments.Update(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(VoiceComment comment, CancellationToken cancellationToken = default)
    {
        _dbContext.VoiceComments.Remove(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Dictionary<DateOnly, int>> GetDailyCountsAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default)
    {
        var dates = await _dbContext.VoiceComments
            .Where(x => x.CreatedAt >= start && x.CreatedAt <= end)
            .Select(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return dates
            .GroupBy(x => DateOnly.FromDateTime(x.ToOffset(TimeSpan.FromHours(7)).DateTime))
            .ToDictionary(g => g.Key, g => g.Count());
    }
}
