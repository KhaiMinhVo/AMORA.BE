using Amora.Domain.Entities;
using Amora.Domain.Interfaces;
using Amora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Amora.Infrastructure.Repositories;

public sealed class PetRepository : IPetRepository
{
    private readonly AmoraDbContext _db;

    public PetRepository(AmoraDbContext db) => _db = db;

    public Task<Pet?> GetByMatchIdAsync(Guid matchId, CancellationToken cancellationToken = default)
        => _db.Pets.FirstOrDefaultAsync(x => x.MatchId == matchId, cancellationToken);

    public Task<Pet?> GetByIdAsync(Guid petId, CancellationToken cancellationToken = default)
        => _db.Pets.FirstOrDefaultAsync(x => x.Id == petId, cancellationToken);

    public async Task AddAsync(Pet pet, CancellationToken cancellationToken = default)
        => await _db.Pets.AddAsync(pet, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _db.SaveChangesAsync(cancellationToken);

    public async Task<IReadOnlyList<Pet>> GetPetsNeedingDecayAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var threshold = DateTimeOffset.UtcNow.AddHours(-6);
        return await _db.Pets
            .Where(x => !x.IsFrozen && x.LastInteractionAt <= threshold)
            .OrderBy(x => x.LastInteractionAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task AddHistoryAsync(PetStateHistory history, CancellationToken cancellationToken = default)
        => await _db.PetStateHistories.AddAsync(history, cancellationToken);

    public async Task<IReadOnlyList<Pet>> GetAllForDailySnapshotAsync(CancellationToken cancellationToken = default)
        => await _db.Pets.Where(x => !x.IsFrozen).ToListAsync(cancellationToken);
}
