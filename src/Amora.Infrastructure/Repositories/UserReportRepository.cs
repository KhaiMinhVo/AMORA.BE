using Amora.Domain.Entities;
using Amora.Domain.Interfaces;
using Amora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Amora.Infrastructure.Repositories;

public sealed class UserReportRepository : IUserReportRepository
{
    private readonly AmoraDbContext _dbContext;

    public UserReportRepository(AmoraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(UserReport report, CancellationToken cancellationToken = default)
    {
        await _dbContext.UserReports.AddAsync(report, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> ExistsRecentAsync(Guid reporterId, Guid targetUserId, Guid? targetPostId, Guid? targetCommentId, DateTimeOffset since, CancellationToken cancellationToken = default)
    {
        return _dbContext.UserReports.AnyAsync(
            x => x.ReporterId == reporterId
                 && x.TargetUserId == targetUserId
                 && x.TargetPostId == targetPostId
                 && x.TargetCommentId == targetCommentId
                 && x.CreatedAt >= since,
            cancellationToken);
    }

    public Task<int> CountReportsAgainstUserAsync(Guid targetUserId, CancellationToken cancellationToken = default)
    {
        return _dbContext.UserReports.CountAsync(x => x.TargetUserId == targetUserId, cancellationToken);
    }

    public Task<UserReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.UserReports.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<(IEnumerable<UserReport> Reports, int TotalCount)> GetReportsAsync(int page, int pageSize, Domain.Enums.ReportStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.UserReports.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var reports = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (reports, totalCount);
    }

    public async Task UpdateAsync(UserReport report, CancellationToken cancellationToken = default)
    {
        _dbContext.UserReports.Update(report);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TryTransitionStatusAsync(Guid reportId, Domain.Enums.ReportStatus expected, Domain.Enums.ReportStatus next, CancellationToken cancellationToken = default)
    {
        var affected = await _dbContext.UserReports
            .Where(x => x.Id == reportId && x.Status == expected)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.Status, next),
                cancellationToken);
        return affected == 1;
    }

    public async Task UpdateAiEvaluationAsync(Guid reportId, string verdict, double? score, DateTimeOffset evaluatedAt, CancellationToken cancellationToken = default)
    {
        await _dbContext.UserReports
            .Where(x => x.Id == reportId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.AiVerdict, verdict)
                    .SetProperty(x => x.AiScore, score)
                    .SetProperty(x => x.AiEvaluatedAt, evaluatedAt),
                cancellationToken);
    }

    public Task<int> CountPendingReportsAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.UserReports.CountAsync(r => r.Status == Amora.Domain.Enums.ReportStatus.Pending, cancellationToken);
    }

    public Task<int> CountAllReportsAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.UserReports.CountAsync(cancellationToken);
    }
}
