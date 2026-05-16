using Amora.Domain.Entities;

namespace Amora.Domain.Interfaces;

public interface IShopRepository
{
    Task<IReadOnlyList<ShopItem>> GetActiveItemsAsync(CancellationToken cancellationToken = default);

    Task<ShopItem?> GetItemByIdAsync(Guid itemId, CancellationToken cancellationToken = default);

    Task<ShopItem?> GetItemByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserInventory>> GetInventoryAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserInventory?> GetInventorySlotAsync(Guid userId, Guid shopItemId, CancellationToken cancellationToken = default);

    Task AddInventoryAsync(UserInventory slot, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
