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

    public Task<bool> ExistsAsync(Guid reporterId, Guid targetUserId, CancellationToken cancellationToken = default)
    {
        return _dbContext.UserReports.AnyAsync(
            x => x.ReporterId == reporterId && x.TargetUserId == targetUserId,
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

    public Task<int> CountPendingReportsAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.UserReports.CountAsync(r => r.Status == Amora.Domain.Enums.ReportStatus.Pending, cancellationToken);
    }
}
