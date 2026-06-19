using Amora.Domain.Entities;
using Amora.Domain.Interfaces;
using Amora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Amora.Infrastructure.Repositories;

public sealed class PaymentTransactionRepository : IPaymentTransactionRepository
{
    private readonly AmoraDbContext _context;

    public PaymentTransactionRepository(AmoraDbContext context) => _context = context;

    public async Task AddAsync(PaymentTransaction transaction, CancellationToken cancellationToken)
    {
        await _context.PaymentTransactions.AddAsync(transaction, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PaymentTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.PaymentTransactions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<PaymentTransaction?> GetByOrderCodeAsync(long orderCode, CancellationToken cancellationToken)
    {
        return await _context.PaymentTransactions.FirstOrDefaultAsync(x => x.OrderCode == orderCode, cancellationToken);
    }

    public async Task UpdateAsync(PaymentTransaction transaction, CancellationToken cancellationToken)
    {
        _context.PaymentTransactions.Update(transaction);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PaymentTransaction>> GetPendingPayOsTransactionsAsync(DateTime olderThan, CancellationToken cancellationToken)
    {
        return await _context.PaymentTransactions
            .Where(x => x.Status == Amora.Domain.Enums.PaymentTransactionStatus.Pending && 
                        x.Provider == "PayOS" && 
                        x.CreatedAt < olderThan)
            .ToListAsync(cancellationToken);
    }
}
