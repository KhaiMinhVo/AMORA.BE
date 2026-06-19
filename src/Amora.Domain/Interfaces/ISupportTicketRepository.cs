using Amora.Domain.Entities;

namespace Amora.Domain.Interfaces;

public interface ISupportTicketRepository
{
    Task<SupportTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SupportTicket?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<SupportTicket>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);
    Task AddAsync(SupportTicket ticket, CancellationToken cancellationToken = default);
    Task UpdateAsync(SupportTicket ticket, CancellationToken cancellationToken = default);
}
