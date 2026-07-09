namespace Amora.Application.Dtos.Pets;

public sealed class PetStatusDto
{
    public Guid PetId { get; init; }
    public string Name { get; init; } = string.Empty;
    public Guid MatchId { get; init; }
    public int Hp { get; init; }
    public int Mood { get; init; }
    public long Rp { get; init; }
    public int VoiceExpToday { get; init; }
    public int MaxVoiceExpPerDay { get; init; } = 50;
    public int Stage { get; init; }
    public string StageName { get; init; } = string.Empty;
    public string Type { get; init; } = "None";
    public string TypeName { get; init; } = string.Empty;
    public string TypeDescription { get; init; } = string.Empty;
    public bool IsFrozen { get; init; }
    public DateTimeOffset ExpiresAtHint { get; init; }
    public IReadOnlyList<string> UnlockedFeatures { get; init; } = Array.Empty<string>();
    public int WaterClaimCountToday { get; init; }
    public int MaxWaterClaimsPerDay { get; init; } = 3;
    public DateTimeOffset? NextWaterClaimAvailableAt { get; init; }
}

public sealed class WaterClaimResultDto
{
    public int WaterClaimCountToday { get; init; }
    public int MaxWaterClaimsPerDay { get; init; } = 3;
    public DateTimeOffset? NextWaterClaimAvailableAt { get; init; }
    public long Rp { get; init; }
}

public sealed class PetActivityDto
{
    public Guid Id { get; init; }
    public string ActivityType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public string UserDisplayName { get; init; } = string.Empty;
}

public sealed class ShopItemDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string ItemType { get; init; } = string.Empty;
    public int PriceDiamonds { get; init; }
    public bool IsActive { get; init; }
    public string? ImageUrl { get; init; }
}

public sealed class InventoryItemDto
{
    public Guid ShopItemId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string ItemType { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public string? ImageUrl { get; init; }
}

public sealed class BuyItemRequest
{
    public Guid ItemId { get; init; }
    public int Quantity { get; init; } = 1;
}

public sealed class UseItemRequest
{
    public Guid ItemId { get; init; }
}

public sealed class TransactionDto
{
    public Guid Id { get; init; }
    public string TransactionType { get; init; } = string.Empty;
    public int DiamondsDelta { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string? ItemName { get; init; }
}
