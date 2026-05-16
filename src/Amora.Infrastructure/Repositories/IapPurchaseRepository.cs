using Amora.Domain.Interfaces;
using Amora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Amora.Infrastructure.Repositories;

public sealed class IapPurchaseRepository : IIapPurchaseRepository
{
    private readonly AmoraDbContext _db;

    public IapPurchaseRepository(AmoraDbContext db) => _db = db;

    public Task<bool> ExistsAsync(string platform, string transactionId, CancellationToken cancellationToken = default)
        => _db.IapPurchaseRecords.AnyAsync(
            x => x.Platform == platform && x.TransactionId == transactionId,
            cancellationToken);

    public async Task AddAsync(Domain.Entities.IapPurchaseRecord record, CancellationToken cancellationToken = default)
        => await _db.IapPurchaseRecords.AddAsync(record, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _db.SaveChangesAsync(cancellationToken);
}
