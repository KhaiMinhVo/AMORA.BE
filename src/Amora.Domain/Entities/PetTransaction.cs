using Amora.Domain.Common;

namespace Amora.Domain.Entities;

public sealed class PetTransaction : BaseEntity
{
    public Guid UserId { get; set; }

    public Guid? ShopItemId { get; set; }

    public string TransactionType { get; set; } = string.Empty;

    public int PetCoinsDelta { get; set; }

    public int AmoraGemsDelta { get; set; }

    public string? MetadataJson { get; set; }

    public AppUser? User { get; set; }

    public ShopItem? ShopItem { get; set; }
}
