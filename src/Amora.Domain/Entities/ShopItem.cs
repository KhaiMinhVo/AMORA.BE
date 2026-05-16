using Amora.Domain.Common;
using Amora.Domain.Enums;

namespace Amora.Domain.Entities;

public sealed class ShopItem : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ItemType ItemType { get; set; }

    public int PricePetCoins { get; set; }

    public int PriceAmoraGems { get; set; }

    /// <summary>JSON hiệu ứng: hp, mood, buffType, rpBonus...</summary>
    public string EffectJson { get; set; } = "{}";

    public bool IsActive { get; set; } = true;
}
