using System.Text.Json;
using Amora.Application.Abstractions;
using Amora.Application.Dtos.Pets;
using Amora.Application.Exceptions;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;
namespace Amora.Application.Pets;

public sealed class PetShopService
{
    private readonly IShopRepository _shopRepository;
    private readonly IPetRepository _petRepository;
    private readonly IPetTransactionRepository _transactionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMatchConnectionRepository _matchRepository;
    private readonly IPetRealtimeNotifier _petNotifier;

    public PetShopService(
        IShopRepository shopRepository,
        IPetRepository petRepository,
        IPetTransactionRepository transactionRepository,
        IUserRepository userRepository,
        IMatchConnectionRepository matchRepository,
        IPetRealtimeNotifier petNotifier)
    {
        _shopRepository = shopRepository;
        _petRepository = petRepository;
        _transactionRepository = transactionRepository;
        _userRepository = userRepository;
        _matchRepository = matchRepository;
        _petNotifier = petNotifier;
    }

    public async Task<IReadOnlyList<ShopItemDto>> GetShopItemsAsync(CancellationToken cancellationToken)
    {
        var items = await _shopRepository.GetActiveItemsAsync(cancellationToken);
        return items.Select(MapItem).ToList();
    }

    public async Task<IReadOnlyList<InventoryItemDto>> GetInventoryAsync(Guid userId, CancellationToken cancellationToken)
    {
        var slots = await _shopRepository.GetInventoryAsync(userId, cancellationToken);
        return slots.Select(s => new InventoryItemDto
        {
            ShopItemId = s.ShopItemId,
            Code = s.ShopItem?.Code ?? string.Empty,
            Name = s.ShopItem?.Name ?? string.Empty,
            Quantity = s.Quantity
        }).ToList();
    }

    public async Task BuyAsync(Guid userId, BuyItemRequest request, CancellationToken cancellationToken)
    {
        var item = await _shopRepository.GetItemByIdAsync(request.ItemId, cancellationToken)
            ?? throw new NotFoundApiException("Item not found.");

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundApiException("User not found.");

        if (request.UseAmoraGems)
        {
            if (user.AmoraGems < item.PriceAmoraGems)
                throw new ValidationApiException("Không đủ Amora Gem.");
            user.AmoraGems -= item.PriceAmoraGems;
        }
        else
        {
            if (user.PetCoins < item.PricePetCoins)
                throw new ValidationApiException("Không đủ Pet Coin.");
            user.PetCoins -= item.PricePetCoins;
        }

        await _userRepository.UpdateAsync(user, cancellationToken);

        var slot = await _shopRepository.GetInventorySlotAsync(userId, item.Id, cancellationToken);
        if (slot is null)
        {
            await _shopRepository.AddInventoryAsync(new UserInventory
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ShopItemId = item.Id,
                Quantity = 1,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }, cancellationToken);
        }
        else
        {
            slot.Quantity++;
            slot.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _transactionRepository.AddAsync(new PetTransaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ShopItemId = item.Id,
            TransactionType = "Purchase",
            PetCoinsDelta = request.UseAmoraGems ? 0 : -item.PricePetCoins,
            AmoraGemsDelta = request.UseAmoraGems ? -item.PriceAmoraGems : 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        await _shopRepository.SaveChangesAsync(cancellationToken);
        await _transactionRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task UseItemAsync(Guid userId, Guid matchId, Guid itemId, CancellationToken cancellationToken)
    {
        if (!await _matchRepository.IsParticipantAsync(matchId, userId, cancellationToken))
            throw new ForbiddenApiException("Not a participant of this match.");

        var slot = await _shopRepository.GetInventorySlotAsync(userId, itemId, cancellationToken)
            ?? throw new ValidationApiException("Item not in inventory.");

        if (slot.Quantity <= 0)
            throw new ValidationApiException("Out of stock.");

        var item = slot.ShopItem ?? await _shopRepository.GetItemByIdAsync(itemId, cancellationToken)
            ?? throw new NotFoundApiException("Item not found.");

        var pet = await _petRepository.GetByMatchIdAsync(matchId, cancellationToken)
            ?? throw new NotFoundApiException("Pet not found for this match.");

        ApplyItemEffect(pet, item);

        slot.Quantity--;
        slot.UpdatedAt = DateTimeOffset.UtcNow;
        pet.UpdatedAt = DateTimeOffset.UtcNow;
        pet.Stage = PetEngine.EvaluateStage(pet);

        await _petRepository.SaveChangesAsync(cancellationToken);
        await _shopRepository.SaveChangesAsync(cancellationToken);

        var match = await _matchRepository.GetByIdAsync(matchId, cancellationToken);
        if (match is not null)
            await _petNotifier.NotifyPetStatusUpdatedAsync(pet, match, cancellationToken);
    }

    private static void ApplyItemEffect(Pet pet, ShopItem item)
    {
        using var doc = JsonDocument.Parse(item.EffectJson);
        var root = doc.RootElement;

        switch (item.Code)
        {
            case "energy_cookie":
                PetEngine.ApplyHpGain(pet, root.GetProperty("hp").GetInt32(), bypassCap: true);
                break;
            case "gentle_bath":
                PetEngine.AddBuff(pet, PetBuffType.AffectionateMood, TimeSpan.FromHours(2));
                pet.Mood = PetMood.Affectionate;
                break;
            case "growth_potion":
                PetEngine.AddBuff(pet, PetBuffType.DoubleVoiceRp, TimeSpan.FromHours(6));
                break;
            case "resonance_candy":
                if (!pet.IsFrozen) pet.Rp += root.GetProperty("rp").GetInt32();
                break;
            case "revival_flask":
                pet.IsFrozen = false;
                pet.Hp = root.GetProperty("hp").GetInt32();
                pet.Mood = PetMood.Neutral;
                break;
            default:
                // cosmetic items — no stat change
                break;
        }
    }

    private static ShopItemDto MapItem(ShopItem item) => new()
    {
        Id = item.Id,
        Code = item.Code,
        Name = item.Name,
        Description = item.Description,
        ItemType = item.ItemType.ToString(),
        PricePetCoins = item.PricePetCoins,
        PriceAmoraGems = item.PriceAmoraGems
    };
}
