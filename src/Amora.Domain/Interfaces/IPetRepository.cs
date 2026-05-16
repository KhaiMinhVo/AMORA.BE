using Amora.Domain.Entities;

namespace Amora.Domain.Interfaces;

public interface IPetRepository
{
    Task<Pet?> GetByMatchIdAsync(Guid matchId, CancellationToken cancellationToken = default);

    Task<Pet?> GetByIdAsync(Guid petId, CancellationToken cancellationToken = default);

    Task AddAsync(Pet pet, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Pet>> GetPetsNeedingDecayAsync(int batchSize, CancellationToken cancellationToken = default);

    Task AddHistoryAsync(PetStateHistory history, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Pet>> GetAllForDailySnapshotAsync(CancellationToken cancellationToken = default);
}
