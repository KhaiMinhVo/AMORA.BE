using Amora.Domain.Entities;

namespace Amora.Domain.Interfaces;

public interface IPaymentTransactionRepository
{
    Task AddAsync(PaymentTransaction transaction, CancellationToken cancellationToken);
    Task<PaymentTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PaymentTransaction?> GetByOrderCodeAsync(long orderCode, CancellationToken cancellationToken);
    Task UpdateAsync(PaymentTransaction transaction, CancellationToken cancellationToken);
    Task<IReadOnlyList<PaymentTransaction>> GetPendingPayOsTransactionsAsync(DateTime olderThan, CancellationToken cancellationToken);
}
