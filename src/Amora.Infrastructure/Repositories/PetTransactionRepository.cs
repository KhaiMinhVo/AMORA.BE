using Amora.Domain.Entities;
using Amora.Domain.Interfaces;
using Amora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Amora.Infrastructure.Repositories;

public sealed class PetTransactionRepository : IPetTransactionRepository
{
    private readonly AmoraDbContext _db;

    public PetTransactionRepository(AmoraDbContext db) => _db = db;

    public async Task AddAsync(PetTransaction transaction, CancellationToken cancellationToken = default)
        => await _db.PetTransactions.AddAsync(transaction, cancellationToken);

    public async Task<IReadOnlyList<PetTransaction>> GetByUserAsync(Guid userId, int limit, CancellationToken cancellationToken = default)
        => await _db.PetTransactions
            .AsNoTracking()
            .Include(x => x.ShopItem)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _db.SaveChangesAsync(cancellationToken);
}
