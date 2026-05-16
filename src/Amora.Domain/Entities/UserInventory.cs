using Amora.Domain.Common;

namespace Amora.Domain.Entities;

public sealed class UserInventory : BaseEntity
{
    public Guid UserId { get; set; }

    public Guid ShopItemId { get; set; }

    public int Quantity { get; set; }

    public AppUser? User { get; set; }

    public ShopItem? ShopItem { get; set; }
}
