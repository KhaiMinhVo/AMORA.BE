using Amora.Domain.Common;
using Amora.Domain.Enums;

namespace Amora.Domain.Entities;

public sealed class ShopItem : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ItemType ItemType { get; set; }

    public int PriceDiamonds { get; set; }

    /// <summary>JSON hiệu ứng: hp, mood, buffType, rpBonus...</summary>
    public string EffectJson { get; set; } = "{}";

    public bool IsActive { get; set; } = true;

    /// <summary>URL hình ảnh vật phẩm.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Giai đoạn tối thiểu để mua/sử dụng (null = tất cả).</summary>
    public GrowthStage? MinStage { get; set; }

    /// <summary>Giới hạn số lần mua trong ngày (0 = không giới hạn).</summary>
    public int DailyPurchaseLimit { get; set; }

}
