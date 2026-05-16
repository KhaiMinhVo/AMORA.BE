using Amora.Domain.Entities;

namespace Amora.Domain.Interfaces;

public interface IIapPurchaseRepository
{
    Task<bool> ExistsAsync(string platform, string transactionId, CancellationToken cancellationToken = default);

    Task AddAsync(IapPurchaseRecord record, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
