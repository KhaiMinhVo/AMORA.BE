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

    public async Task AddActivityAsync(PetActivity activity, CancellationToken cancellationToken = default)
        => await _db.PetActivities.AddAsync(activity, cancellationToken);

    public async Task<IReadOnlyList<PetActivity>> GetActivitiesAsync(Guid matchId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _db.PetActivities
            .Include(x => x.User)
            .Where(x => x.MatchId == matchId)
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }
}
