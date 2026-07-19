using Amora.Domain.Entities;

namespace Amora.Domain.Interfaces;

public interface IUserPushTokenRepository
{
    Task AddOrUpdateAsync(UserPushToken token, CancellationToken cancellationToken = default);
    Task RemoveByDeviceAsync(Guid userId, string deviceId, CancellationToken cancellationToken = default);
    Task RemoveByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserPushToken>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
