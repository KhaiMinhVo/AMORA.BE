using Amora.Domain.Entities;

namespace Amora.Domain.Interfaces;

public interface IUserRepository
{
    Task<AppUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<AppUser?> GetByIdForUpdateAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<AppUser?> GetByEmailForAuthAsync(string email, CancellationToken cancellationToken = default);

    Task<AppUser?> GetByGoogleIdAsync(string googleId, CancellationToken cancellationToken = default);

    Task<AppUser?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);

    Task AddAsync(AppUser user, CancellationToken cancellationToken = default);

    Task UpdateAsync(AppUser user, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<AppUser> Items, int TotalCount)> GetAllUsersAsync(int page, int pageSize, string? keyword = null, CancellationToken cancellationToken = default);
}