using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Amora.Api.Controllers;

[ApiController]
[Route("api/admin/shop")]
[Authorize] // Should be restricted to Admin role in production
public sealed class AdminShopController : ControllerBase
{
    private readonly IShopRepository _shopRepository;

    public AdminShopController(IShopRepository shopRepository)
    {
        _shopRepository = shopRepository;
    }

    /// <summary>Thêm vật phẩm mới vào cửa hàng.</summary>
    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] CreateShopItemRequest request, CancellationToken cancellationToken)
    {
        var item = new ShopItem
        {
            Id = Guid.NewGuid(),
            Code = request.Code.Trim().ToLowerInvariant(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            ItemType = Enum.Parse<ItemType>(request.ItemType, ignoreCase: true),
            PriceDiamonds = request.PriceDiamonds,
            EffectJson = request.EffectJson ?? "{}",
            ImageUrl = request.ImageUrl,
            MinStage = string.IsNullOrWhiteSpace(request.MinStage) ? null : Enum.Parse<GrowthStage>(request.MinStage, ignoreCase: true),
            DailyPurchaseLimit = request.DailyPurchaseLimit,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        return Ok(new { success = true, data = new { item.Id, item.Code, item.Name } });
    }

    /// <summary>Cập nhật vật phẩm.</summary>
    [HttpPut("items/{itemId:guid}")]
    public async Task<IActionResult> UpdateItem(Guid itemId, [FromBody] UpdateShopItemRequest request, CancellationToken cancellationToken)
    {
        var item = await _shopRepository.GetItemByIdAsync(itemId, cancellationToken);
        if (item is null) return NotFound(new { success = false, message = "Item not found." });

        if (!string.IsNullOrWhiteSpace(request.Name)) item.Name = request.Name.Trim();
        if (!string.IsNullOrWhiteSpace(request.Description)) item.Description = request.Description.Trim();
        if (request.PriceDiamonds.HasValue) item.PriceDiamonds = request.PriceDiamonds.Value;
        if (!string.IsNullOrWhiteSpace(request.EffectJson)) item.EffectJson = request.EffectJson;
        if (request.ImageUrl is not null) item.ImageUrl = request.ImageUrl;
        if (request.IsActive.HasValue) item.IsActive = request.IsActive.Value;
        if (request.DailyPurchaseLimit.HasValue) item.DailyPurchaseLimit = request.DailyPurchaseLimit.Value;

        await _shopRepository.SaveChangesAsync(cancellationToken);
        return Ok(new { success = true, data = new { item.Id, item.Code, item.Name } });
    }

    /// <summary>Khởi tạo toàn bộ dữ liệu Shop.</summary>
    [HttpPost("seed-items")]
    public async Task<IActionResult> SeedItems(CancellationToken cancellationToken)
    {
        var items = new List<ShopItem>
        {
            // THÚ NON
            CreateItem("bang_do_no", "Băng đô nơ", ItemType.Cosmetic, 10, "{\"slot\":\"hat\"}", null, null),
            CreateItem("non_ca_tim", "Nón cà tím", ItemType.Cosmetic, 15, "{\"slot\":\"hat\"}", null, null),
            CreateItem("kinh_mat_tron", "Kính mắt tròn", ItemType.Cosmetic, 10, "{\"slot\":\"glasses\"}", null, null),
            CreateItem("bim_bong_mem", "Bỉm bông mềm", ItemType.Cosmetic, 20, "{\"slot\":\"outfit\"}", null, null),
            CreateItem("pet_food", "Thức ăn", ItemType.Consumable, 5, "{\"hp\":10}", null, null),
            CreateItem("water", "Nước", ItemType.Consumable, 0, "{\"hp\":5}", null, 3), // Limit 3 per day
            CreateItem("qua_bong", "Quả bóng", ItemType.Toy, 8, "{\"rp\":15}", null, 2),
            CreateItem("can_cau_meo", "Cần câu mèo", ItemType.Toy, 10, "{\"rp\":20}", null, 2),
            CreateItem("revival_potion", "Thuốc hồi sinh", ItemType.Revival, 50, "{\"revive\":true,\"hp\":50}", null, null),
            CreateItem("exp_booster", "Vitamin Tăng Trưởng", ItemType.Buff, 16, "{\"buff\":\"DoubleAllExp\",\"minutes\":120}", null, null),
            CreateItem("rename_card", "Thẻ đổi tên", ItemType.Special, 30, "{\"rename\":true}", null, null),

            // THIẾU NIÊN
            CreateItem("mu_xo_bong", "Mũ xô bông", ItemType.Cosmetic, 50, "{\"slot\":\"hat\"}", GrowthStage.Young, null),
            CreateItem("bo_yem", "Bộ yếm", ItemType.Cosmetic, 80, "{\"slot\":\"outfit\"}", GrowthStage.Young, null),
            CreateItem("bo_khung_long", "Bộ khủng long", ItemType.Cosmetic, 100, "{\"slot\":\"outfit\"}", GrowthStage.Young, null),

            // TRƯỞNG THÀNH
            CreateItem("mu_bong_chay", "Mũ bóng chày", ItemType.Cosmetic, 60, "{\"slot\":\"hat\"}", GrowthStage.Adult, null),
            CreateItem("ao_lien_quan", "Bộ áo liền quần", ItemType.Cosmetic, 120, "{\"slot\":\"outfit\"}", GrowthStage.Adult, null),
            CreateItem("kinh_mat", "Kính mát", ItemType.Cosmetic, 30, "{\"slot\":\"glasses\"}", GrowthStage.Adult, null),
            CreateItem("non_hong", "Nón hồng", ItemType.Cosmetic, 50, "{\"slot\":\"hat\"}", GrowthStage.Adult, null),
            CreateItem("suit_the_thao", "Suit thể thao", ItemType.Cosmetic, 100, "{\"slot\":\"outfit\"}", GrowthStage.Adult, null)
        };

        foreach (var item in items)
        {
            var existing = await _shopRepository.GetItemByCodeAsync(item.Code, cancellationToken);
            if (existing == null)
            {
                await _shopRepository.AddItemAsync(item, cancellationToken);
            }
        }

        await _shopRepository.SaveChangesAsync(cancellationToken);
        return Ok(new { success = true, count = items.Count });
    }

    private static ShopItem CreateItem(string code, string name, ItemType type, int price, string effect, GrowthStage? stage, int? limit)
    {
        return new ShopItem
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            ItemType = type,
            PriceDiamonds = price,
            EffectJson = effect,
            MinStage = stage,
            DailyPurchaseLimit = limit ?? 0,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}

public sealed class UpdateShopItemRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? PriceDiamonds { get; set; }
    public string? EffectJson { get; set; }
    public string? ImageUrl { get; set; }
    public bool? IsActive { get; set; }
    public int? DailyPurchaseLimit { get; set; }
}

public sealed class CreateShopItemRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ItemType { get; set; } = "Consumable";
    public int PriceDiamonds { get; set; }
    public string? EffectJson { get; set; }
    public string? ImageUrl { get; set; }
    public string? MinStage { get; set; }
    public int DailyPurchaseLimit { get; set; }
}
