using Amora.Domain.Entities;

namespace Amora.Domain.Interfaces;

public interface IPetTransactionRepository
{
    Task AddAsync(PetTransaction transaction, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PetTransaction>> GetByUserAsync(Guid userId, int limit, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
