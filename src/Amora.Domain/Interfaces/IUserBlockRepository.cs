using Amora.Domain.Entities;

namespace Amora.Domain.Interfaces;

public interface IUserBlockRepository
{
    Task AddAsync(UserBlock block, CancellationToken cancellationToken = default);

    Task RemoveAsync(Guid blockerId, Guid blockedUserId, CancellationToken cancellationToken = default);

    /// <summary>Kiểm tra A có đang block B không.</summary>
    Task<bool> IsBlockedAsync(Guid blockerId, Guid blockedUserId, CancellationToken cancellationToken = default);

    /// <summary>Lấy danh sách ID mà user đã block (dùng để filter Feed).</summary>
    Task<IReadOnlyList<Guid>> GetBlockedUserIdsAsync(Guid blockerId, CancellationToken cancellationToken = default);
}
