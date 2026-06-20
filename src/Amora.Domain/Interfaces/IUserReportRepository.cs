using Amora.Domain.Entities;

namespace Amora.Domain.Interfaces;

public interface IUserReportRepository
{
    Task AddAsync(UserReport report, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid reporterId, Guid targetUserId, CancellationToken cancellationToken = default);

    Task<int> CountReportsAgainstUserAsync(Guid targetUserId, CancellationToken cancellationToken = default);

    Task<UserReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IEnumerable<UserReport> Reports, int TotalCount)> GetReportsAsync(int page, int pageSize, Amora.Domain.Enums.ReportStatus? status = null, CancellationToken cancellationToken = default);

    Task UpdateAsync(UserReport report, CancellationToken cancellationToken = default);
}
