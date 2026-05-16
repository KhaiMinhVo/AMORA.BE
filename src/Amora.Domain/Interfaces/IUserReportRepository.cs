using Amora.Domain.Entities;

namespace Amora.Domain.Interfaces;

public interface IUserReportRepository
{
    Task AddAsync(UserReport report, CancellationToken cancellationToken = default);

    /// <summary>Kiểm tra user đã report target chưa (tránh spam report).</summary>
    Task<bool> ExistsAsync(Guid reporterId, Guid targetUserId, CancellationToken cancellationToken = default);
}
