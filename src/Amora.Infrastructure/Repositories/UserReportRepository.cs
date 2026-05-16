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
}
