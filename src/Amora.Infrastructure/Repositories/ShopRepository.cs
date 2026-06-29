using Amora.Domain.Entities;
using Amora.Domain.Interfaces;
using Amora.Domain.Enums;
using Amora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Amora.Infrastructure.Repositories;

public sealed class ShopRepository : IShopRepository
{
    private readonly AmoraDbContext _db;

    public ShopRepository(AmoraDbContext db) => _db = db;

    public async Task<IReadOnlyList<ShopItem>> GetActiveItemsAsync(CancellationToken cancellationToken = default)
        => await _db.ShopItems.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ShopItem>> GetAllItemsAsync(CancellationToken cancellationToken = default)
        => await _db.ShopItems.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);

    public Task<ShopItem?> GetItemByIdAsync(Guid itemId, CancellationToken cancellationToken = default)
        => _db.ShopItems.FirstOrDefaultAsync(x => x.Id == itemId, cancellationToken);

    public Task<ShopItem?> GetItemByCodeAsync(string code, CancellationToken cancellationToken = default)
        => _db.ShopItems.FirstOrDefaultAsync(x => x.Code == code, cancellationToken);

    public Task<ShopItem?> GetItemByTypeAsync(ItemType type, CancellationToken cancellationToken = default)
        => _db.ShopItems.FirstOrDefaultAsync(x => x.ItemType == type, cancellationToken);

    public async Task<IReadOnlyList<UserInventory>> GetInventoryAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _db.UserInventories
            .Include(x => x.ShopItem)
            .Where(x => x.UserId == userId && x.Quantity > 0)
            .ToListAsync(cancellationToken);

    public Task<UserInventory?> GetInventorySlotAsync(Guid userId, Guid shopItemId, CancellationToken cancellationToken = default)
        => _db.UserInventories
            .Include(x => x.ShopItem)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ShopItemId == shopItemId, cancellationToken);

    public async Task AddInventoryAsync(UserInventory slot, CancellationToken cancellationToken = default)
    {
        await _db.UserInventories.AddAsync(slot, cancellationToken);
    }

    public async Task AddItemAsync(ShopItem item, CancellationToken cancellationToken = default)
    {
        await _db.ShopItems.AddAsync(item, cancellationToken);
    }

    public async Task AddItemsAsync(IEnumerable<ShopItem> items, CancellationToken cancellationToken = default)
    {
        await _db.ShopItems.AddRangeAsync(items, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _db.SaveChangesAsync(cancellationToken);
}
