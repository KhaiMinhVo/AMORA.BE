using Amora.Domain.Entities;

namespace Amora.Domain.Interfaces;

public interface IUserReportRepository
{
    Task AddAsync(UserReport report, CancellationToken cancellationToken = default);

    Task<bool> ExistsRecentAsync(Guid reporterId, Guid targetUserId, Guid? targetPostId, Guid? targetCommentId, DateTimeOffset since, CancellationToken cancellationToken = default);

    Task<int> CountReportsAgainstUserAsync(Guid targetUserId, CancellationToken cancellationToken = default);

    Task<UserReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IEnumerable<UserReport> Reports, int TotalCount)> GetReportsAsync(int page, int pageSize, Amora.Domain.Enums.ReportStatus? status = null, CancellationToken cancellationToken = default);

    Task UpdateAsync(UserReport report, CancellationToken cancellationToken = default);

    Task<bool> TryTransitionStatusAsync(Guid reportId, Amora.Domain.Enums.ReportStatus expected, Amora.Domain.Enums.ReportStatus next, CancellationToken cancellationToken = default);

    Task UpdateAiEvaluationAsync(Guid reportId, string verdict, double? score, DateTimeOffset evaluatedAt, CancellationToken cancellationToken = default);

    Task<int> CountPendingReportsAsync(CancellationToken cancellationToken = default);

    Task<int> CountAllReportsAsync(CancellationToken cancellationToken = default);
}
