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

        if (user.Diamonds < (item.PriceDiamonds * request.Quantity))
            throw new ValidationApiException("Không đủ Diamonds.");

        user.Diamonds -= item.PriceDiamonds * request.Quantity;

        if (item.ItemType == ItemType.Subscription)
        {
            ApplySubscription(user, item, request.Quantity);
        }
        else
        {
            var slot = await _shopRepository.GetInventorySlotAsync(userId, item.Id, cancellationToken);
            if (slot is null)
            {
                await _shopRepository.AddInventoryAsync(new UserInventory
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ShopItemId = item.Id,
                    Quantity = request.Quantity,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                }, cancellationToken);
            }
            else
            {
                slot.Quantity += request.Quantity;
                slot.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        await _userRepository.UpdateAsync(user, cancellationToken);

        await _transactionRepository.AddAsync(new PetTransaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ShopItemId = item.Id,
            TransactionType = "Purchase",
            DiamondsDelta = -(item.PriceDiamonds * request.Quantity),
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

        if (item.MinStage.HasValue && item.MinStage.Value > pet.Stage)
            throw new ValidationApiException($"Bạn cần Thú cưng đạt mức {item.MinStage.Value} để sử dụng vật phẩm này.");

        ApplyItemEffect(pet, item);

        // Do not consume cosmetics or special items (handled separately or persistent)
        if (item.ItemType != ItemType.Cosmetic)
        {
            slot.Quantity--;
            slot.UpdatedAt = DateTimeOffset.UtcNow;
        }

        pet.UpdatedAt = DateTimeOffset.UtcNow;
        pet.Stage = PetEngine.EvaluateStage(pet);

        await _petRepository.SaveChangesAsync(cancellationToken);
        await _shopRepository.SaveChangesAsync(cancellationToken);

        var match = await _matchRepository.GetByIdAsync(matchId, cancellationToken);
        if (match is not null)
            await _petNotifier.NotifyPetStatusUpdatedAsync(pet, match, cancellationToken);
    }

    public async Task ClaimWaterAsync(Guid userId, Guid matchId, CancellationToken cancellationToken)
    {
        if (!await _matchRepository.IsParticipantAsync(matchId, userId, cancellationToken))
            throw new ForbiddenApiException("Not a participant of this match.");

        var pet = await _petRepository.GetByMatchIdAsync(matchId, cancellationToken)
            ?? throw new NotFoundApiException("Pet not found for this match.");

        // Reset if new day
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (pet.WaterClaimDate < today)
        {
            pet.WaterClaimsToday = 0;
            pet.WaterClaimDate = today;
        }

        if (pet.WaterClaimsToday >= 3)
            throw new ValidationApiException("Bạn đã nhận đủ 3 bình nước hôm nay.");

        if (pet.LastWaterClaimAt.HasValue && (DateTimeOffset.UtcNow - pet.LastWaterClaimAt.Value).TotalHours < 1)
            throw new ValidationApiException("Vui lòng chờ 1 tiếng kể từ lần nhận nước trước.");

        pet.WaterClaimsToday++;
        pet.LastWaterClaimAt = DateTimeOffset.UtcNow;
        if (!pet.IsFrozen && !pet.IsDead) pet.Rp += 5; // Water gives 5 EXP

        pet.Stage = PetEngine.EvaluateStage(pet);
        pet.UpdatedAt = DateTimeOffset.UtcNow;

        await _petRepository.SaveChangesAsync(cancellationToken);

        var match = await _matchRepository.GetByIdAsync(matchId, cancellationToken);
        if (match is not null)
            await _petNotifier.NotifyPetStatusUpdatedAsync(pet, match, cancellationToken);
    }

    public async Task RenamePetAsync(Guid userId, Guid matchId, Guid itemId, string newName, CancellationToken cancellationToken)
    {
        if (!await _matchRepository.IsParticipantAsync(matchId, userId, cancellationToken))
            throw new ForbiddenApiException("Not a participant of this match.");

        var slot = await _shopRepository.GetInventorySlotAsync(userId, itemId, cancellationToken)
            ?? throw new ValidationApiException("Item not in inventory.");

        if (slot.Quantity <= 0)
            throw new ValidationApiException("Out of stock.");

        var item = slot.ShopItem ?? await _shopRepository.GetItemByIdAsync(itemId, cancellationToken);
        if (item == null || item.Code != "rename_card")
            throw new ValidationApiException("Vật phẩm không hợp lệ (cần Thẻ đổi tên).");

        if (string.IsNullOrWhiteSpace(newName) || newName.Length > 30)
            throw new ValidationApiException("Tên không hợp lệ (tối đa 30 ký tự).");

        var pet = await _petRepository.GetByMatchIdAsync(matchId, cancellationToken)
            ?? throw new NotFoundApiException("Pet not found for this match.");

        pet.Name = newName.Trim();
        
        slot.Quantity--;
        slot.UpdatedAt = DateTimeOffset.UtcNow;
        pet.UpdatedAt = DateTimeOffset.UtcNow;

        await _petRepository.SaveChangesAsync(cancellationToken);
        await _shopRepository.SaveChangesAsync(cancellationToken);

        var match = await _matchRepository.GetByIdAsync(matchId, cancellationToken);
        if (match is not null)
            await _petNotifier.NotifyPetStatusUpdatedAsync(pet, match, cancellationToken);
    }

    public async Task EquipCosmeticAsync(Guid userId, Guid matchId, Guid itemId, CancellationToken cancellationToken)
    {
        if (!await _matchRepository.IsParticipantAsync(matchId, userId, cancellationToken))
            throw new ForbiddenApiException("Not a participant of this match.");

        var slot = await _shopRepository.GetInventorySlotAsync(userId, itemId, cancellationToken)
            ?? throw new ValidationApiException("Item not in inventory.");

        if (slot.Quantity <= 0)
            throw new ValidationApiException("Bạn không sở hữu phụ kiện này.");

        var item = slot.ShopItem ?? await _shopRepository.GetItemByIdAsync(itemId, cancellationToken);
        if (item == null || item.ItemType != ItemType.Cosmetic)
            throw new ValidationApiException("Vật phẩm không phải là phụ kiện.");

        var pet = await _petRepository.GetByMatchIdAsync(matchId, cancellationToken)
            ?? throw new NotFoundApiException("Pet not found for this match.");

        if (item.MinStage.HasValue && item.MinStage.Value > pet.Stage)
            throw new ValidationApiException($"Thú cưng cần đạt mức {item.MinStage.Value} để mặc.");

        // Read current cosmetics
        var cosmetics = new List<EquippedCosmetic>();
        if (!string.IsNullOrWhiteSpace(pet.EquippedCosmeticsJson))
        {
            cosmetics = JsonSerializer.Deserialize<List<EquippedCosmetic>>(pet.EquippedCosmeticsJson) ?? new List<EquippedCosmetic>();
        }

        // Get slot from effect json
        var slotId = "outfit";
        if (!string.IsNullOrWhiteSpace(item.EffectJson))
        {
            using var doc = JsonDocument.Parse(item.EffectJson);
            if (doc.RootElement.TryGetProperty("slot", out var s))
                slotId = s.GetString() ?? "outfit";
        }

        // Remove old cosmetic in the same slot
        cosmetics.RemoveAll(c => c.SlotId == slotId);

        // Add new cosmetic
        cosmetics.Add(new EquippedCosmetic { SlotId = slotId, ItemId = item.Id, Code = item.Code });
        
        pet.EquippedCosmeticsJson = JsonSerializer.Serialize(cosmetics);
        pet.UpdatedAt = DateTimeOffset.UtcNow;

        await _petRepository.SaveChangesAsync(cancellationToken);

        var match = await _matchRepository.GetByIdAsync(matchId, cancellationToken);
        if (match is not null)
            await _petNotifier.NotifyPetStatusUpdatedAsync(pet, match, cancellationToken);
    }

    private sealed class EquippedCosmetic
    {
        public string SlotId { get; set; } = string.Empty;
        public Guid ItemId { get; set; }
        public string Code { get; set; } = string.Empty;
    }

    private static void ApplyItemEffect(Pet pet, ShopItem item)
    {
        if (string.IsNullOrWhiteSpace(item.EffectJson) || item.EffectJson == "{}") return;

        using var doc = JsonDocument.Parse(item.EffectJson);
        var root = doc.RootElement;

        if (root.TryGetProperty("revive", out var reviveProp) && reviveProp.GetBoolean())
        {
            pet.IsDead = false;
            pet.IsFrozen = false;
        }

        if (root.TryGetProperty("hp", out var hpProp))
        {
            PetEngine.ApplyHpGain(pet, hpProp.GetInt32(), bypassCap: true);
        }

        if (root.TryGetProperty("rp", out var rpProp))
        {
            if (!pet.IsFrozen && !pet.IsDead) pet.Rp += rpProp.GetInt32();
        }

        if (root.TryGetProperty("buff", out var buffProp))
        {
            if (Enum.TryParse<PetBuffType>(buffProp.GetString(), ignoreCase: true, out var buffType))
            {
                var minutes = root.TryGetProperty("minutes", out var m) ? m.GetInt32() : 60;
                PetEngine.AddBuff(pet, buffType, TimeSpan.FromMinutes(minutes));
            }
        }
    }

    private static void ApplySubscription(AppUser user, ShopItem item, int quantity)
    {
        using var doc = JsonDocument.Parse(item.EffectJson);
        var root = doc.RootElement;
        
        if (root.TryGetProperty("premium_days", out var premiumDays))
        {
            user.SubscriptionType = SubscriptionType.Premium;
            var currentEnd = user.SubscriptionEndDate.HasValue && user.SubscriptionEndDate.Value > DateTimeOffset.UtcNow 
                ? user.SubscriptionEndDate.Value 
                : DateTimeOffset.UtcNow;
            user.SubscriptionEndDate = currentEnd.AddDays(premiumDays.GetInt32() * quantity);
        }
        else if (root.TryGetProperty("gold_days", out var goldDays))
        {
            user.SubscriptionType = SubscriptionType.Gold;
            var currentEnd = user.SubscriptionEndDate.HasValue && user.SubscriptionEndDate.Value > DateTimeOffset.UtcNow 
                ? user.SubscriptionEndDate.Value 
                : DateTimeOffset.UtcNow;
            user.SubscriptionEndDate = currentEnd.AddDays(goldDays.GetInt32() * quantity);
        }
    }

    private static ShopItemDto MapItem(ShopItem item) => new()
    {
        Id = item.Id,
        Code = item.Code,
        Name = item.Name,
        Description = item.Description,
        ItemType = item.ItemType.ToString(),
        PriceDiamonds = item.PriceDiamonds,
        IsActive = item.IsActive
    };
}
