namespace Amora.Application.Dtos.Pets;

public sealed class PetStatusDto
{
    public Guid PetId { get; init; }
    public Guid MatchId { get; init; }
    public int Hp { get; init; }
    public string Mood { get; init; } = string.Empty;
    public long Rp { get; init; }
    public int Stage { get; init; }
    public string StageName { get; init; } = string.Empty;
    public bool IsFrozen { get; init; }
    public DateTimeOffset ExpiresAtHint { get; init; }
    public IReadOnlyList<string> UnlockedFeatures { get; init; } = Array.Empty<string>();
}

public sealed class ShopItemDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string ItemType { get; init; } = string.Empty;
    public int PricePetCoins { get; init; }
    public int PriceAmoraGems { get; init; }
}

public sealed class InventoryItemDto
{
    public Guid ShopItemId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int Quantity { get; init; }
}

public sealed class BuyItemRequest
{
    public Guid ItemId { get; init; }
    public bool UseAmoraGems { get; init; }
}

public sealed class UseItemRequest
{
    public Guid ItemId { get; init; }
}

public sealed class TransactionDto
{
    public Guid Id { get; init; }
    public string TransactionType { get; init; } = string.Empty;
    public int PetCoinsDelta { get; init; }
    public int AmoraGemsDelta { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string? ItemName { get; init; }
}
