using Amora.Application.Abstractions;
using Amora.Application.Dtos.Pets;
using Amora.Application.Exceptions;
using Amora.Application.Pets;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace Amora.Application.Tests.Pets;

public class PetShopServiceTests
{
    private readonly Mock<IShopRepository> _mockShopRepository;
    private readonly Mock<IPetRepository> _mockPetRepository;
    private readonly Mock<IPetTransactionRepository> _mockTransactionRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IMatchConnectionRepository> _mockMatchRepository;
    private readonly Mock<IPetRealtimeNotifier> _mockPetNotifier;
    private readonly PetShopService _petShopService;

    public PetShopServiceTests()
    {
        _mockShopRepository = new Mock<IShopRepository>();
        _mockPetRepository = new Mock<IPetRepository>();
        _mockTransactionRepository = new Mock<IPetTransactionRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockMatchRepository = new Mock<IMatchConnectionRepository>();
        _mockPetNotifier = new Mock<IPetRealtimeNotifier>();

        _petShopService = new PetShopService(
            _mockShopRepository.Object,
            _mockPetRepository.Object,
            _mockTransactionRepository.Object,
            _mockUserRepository.Object,
            _mockMatchRepository.Object,
            _mockPetNotifier.Object
        );
    }

    [Fact]
    public async Task BuyAsync_ShouldThrowValidationException_WhenNotEnoughCoins()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var user = new AppUser { Id = userId, PetCoins = 50 };
        var item = new ShopItem { Id = itemId, PricePetCoins = 100 };

        _mockShopRepository.Setup(s => s.GetItemByIdAsync(itemId, It.IsAny<CancellationToken>())).ReturnsAsync(item);
        _mockUserRepository.Setup(u => u.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        // Act
        var act = async () => await _petShopService.BuyAsync(userId, new BuyItemRequest { ItemId = itemId, UseAmoraGems = false }, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationApiException>().WithMessage("Không đủ Pet Coin.");
    }

    [Fact]
    public async Task BuyAsync_ShouldThrowValidationException_WhenNotEnoughGems()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var user = new AppUser { Id = userId, AmoraGems = 5 };
        var item = new ShopItem { Id = itemId, PriceAmoraGems = 10 };

        _mockShopRepository.Setup(s => s.GetItemByIdAsync(itemId, It.IsAny<CancellationToken>())).ReturnsAsync(item);
        _mockUserRepository.Setup(u => u.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        // Act
        var act = async () => await _petShopService.BuyAsync(userId, new BuyItemRequest { ItemId = itemId, UseAmoraGems = true }, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationApiException>().WithMessage("Không đủ Amora Gem.");
    }

    [Fact]
    public async Task BuyAsync_ShouldDeductCoinsAndAddInventory_WhenValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var user = new AppUser { Id = userId, PetCoins = 100 };
        var item = new ShopItem { Id = itemId, PricePetCoins = 50 };

        _mockShopRepository.Setup(s => s.GetItemByIdAsync(itemId, It.IsAny<CancellationToken>())).ReturnsAsync(item);
        _mockUserRepository.Setup(u => u.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _mockShopRepository.Setup(s => s.GetInventorySlotAsync(userId, itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserInventory)null!);

        // Act
        await _petShopService.BuyAsync(userId, new BuyItemRequest { ItemId = itemId, UseAmoraGems = false }, CancellationToken.None);

        // Assert
        user.PetCoins.Should().Be(50);
        _mockUserRepository.Verify(u => u.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _mockShopRepository.Verify(s => s.AddInventoryAsync(It.Is<UserInventory>(inv => 
            inv.UserId == userId && inv.ShopItemId == itemId && inv.Quantity == 1
        ), It.IsAny<CancellationToken>()), Times.Once);
        _mockTransactionRepository.Verify(t => t.AddAsync(It.Is<PetTransaction>(pt => 
            pt.UserId == userId && pt.ShopItemId == itemId && pt.TransactionType == "Purchase" && pt.PetCoinsDelta == -50
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UseItemAsync_ShouldThrowForbidden_WhenNotParticipant()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var matchId = Guid.NewGuid();
        _mockMatchRepository.Setup(m => m.IsParticipantAsync(matchId, userId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var act = async () => await _petShopService.UseItemAsync(userId, matchId, Guid.NewGuid(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenApiException>().WithMessage("Not a participant of this match.");
    }

    [Fact]
    public async Task UseItemAsync_ShouldThrowValidation_WhenOutOfStock()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var matchId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        _mockMatchRepository.Setup(m => m.IsParticipantAsync(matchId, userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _mockShopRepository.Setup(s => s.GetInventorySlotAsync(userId, itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInventory { Quantity = 0 });

        // Act
        var act = async () => await _petShopService.UseItemAsync(userId, matchId, itemId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationApiException>().WithMessage("Out of stock.");
    }

    [Fact]
    public async Task UseItemAsync_ShouldApplyEffectAndDecreaseQuantity_WhenValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var matchId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        
        var item = new ShopItem { Id = itemId, Code = "energy_cookie", EffectJson = "{\"hp\": 20}" };
        var slot = new UserInventory { ShopItem = item, Quantity = 2 };
        var pet = new Pet { Id = Guid.NewGuid(), Hp = 50 };

        _mockMatchRepository.Setup(m => m.IsParticipantAsync(matchId, userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _mockShopRepository.Setup(s => s.GetInventorySlotAsync(userId, itemId, It.IsAny<CancellationToken>())).ReturnsAsync(slot);
        _mockPetRepository.Setup(p => p.GetByMatchIdAsync(matchId, It.IsAny<CancellationToken>())).ReturnsAsync(pet);

        // Act
        await _petShopService.UseItemAsync(userId, matchId, itemId, CancellationToken.None);

        // Assert
        slot.Quantity.Should().Be(1);
        pet.Hp.Should().Be(70); // 50 + 20
        _mockPetRepository.Verify(p => p.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockShopRepository.Verify(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
