using Amora.Application.Iap;
using Amora.Domain.Entities;
using Amora.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Amora.Application.Tests.Iap;

public class IapWebhookServiceTests
{
    private readonly Mock<IIapPurchaseRepository> _mockIapRepo;
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IPetTransactionRepository> _mockTransactionRepo;
    private readonly Mock<IIapWebhookEventRepository> _mockWebhookEventRepo;
    private readonly Mock<ILogger<IapWebhookService>> _mockLogger;
    private readonly IapWebhookService _webhookService;

    public IapWebhookServiceTests()
    {
        _mockIapRepo = new Mock<IIapPurchaseRepository>();
        _mockUserRepo = new Mock<IUserRepository>();
        _mockTransactionRepo = new Mock<IPetTransactionRepository>();
        _mockWebhookEventRepo = new Mock<IIapWebhookEventRepository>();
        _mockLogger = new Mock<ILogger<IapWebhookService>>();

        _webhookService = new IapWebhookService(
            _mockIapRepo.Object,
            _mockUserRepo.Object,
            _mockTransactionRepo.Object,
            _mockWebhookEventRepo.Object,
            _mockLogger.Object,
            null!
        );
    }

    [Fact]
    public async Task TryRecordWebhookEventAsync_ShouldReturnTrue_WhenEventAlreadyExists()
    {
        _mockWebhookEventRepo.Setup(w => w.ExistsAsync("Apple", "evt_123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _webhookService.TryRecordWebhookEventAsync("Apple", "evt_123", "Refund", "txn_123", null, CancellationToken.None);

        result.Should().BeTrue();
        _mockWebhookEventRepo.Verify(w => w.AddAsync(It.IsAny<IapWebhookEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TryRecordWebhookEventAsync_ShouldAddEventAndReturnFalse_WhenNewEvent()
    {
        _mockWebhookEventRepo.Setup(w => w.ExistsAsync("Apple", "evt_123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _webhookService.TryRecordWebhookEventAsync("Apple", "evt_123", "Refund", "txn_123", "payload", CancellationToken.None);

        result.Should().BeFalse();
        _mockWebhookEventRepo.Verify(w => w.AddAsync(It.Is<IapWebhookEvent>(e => 
            e.Platform == "Apple" && e.EventId == "evt_123" && e.EventType == "Refund"
        ), It.IsAny<CancellationToken>()), Times.Once);
        _mockWebhookEventRepo.Verify(w => w.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleRefundAsync_ShouldReturnFalse_WhenTransactionNotFound()
    {
        _mockIapRepo.Setup(i => i.GetByPlatformTransactionIdAsync("Apple", "txn_123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IapPurchaseRecord)null!);

        var result = await _webhookService.HandleRefundAsync("Apple", "txn_123", "User requested", CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRefundAsync_ShouldReturnTrue_WhenAlreadyRefunded()
    {
        var record = new IapPurchaseRecord { RefundedAt = DateTimeOffset.UtcNow };
        _mockIapRepo.Setup(i => i.GetByPlatformTransactionIdAsync("Apple", "txn_123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        var result = await _webhookService.HandleRefundAsync("Apple", "txn_123", "User requested", CancellationToken.None);

        result.Should().BeTrue();
        _mockUserRepo.Verify(u => u.UpdateAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleRefundAsync_ShouldReturnFalse_WhenUserNotFound()
    {
        var record = new IapPurchaseRecord { UserId = Guid.NewGuid() };
        _mockIapRepo.Setup(i => i.GetByPlatformTransactionIdAsync("Apple", "txn_123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);
        _mockUserRepo.Setup(u => u.GetByIdAsync(record.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser)null!);

        var result = await _webhookService.HandleRefundAsync("Apple", "txn_123", "User requested", CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRefundAsync_ShouldDeductGemsAndRecordTransaction_WhenValid()
    {
        var userId = Guid.NewGuid();
        var record = new IapPurchaseRecord { UserId = userId, GemsGranted = 100 };
        var user = new AppUser { Id = userId, Diamonds = 150 };

        _mockIapRepo.Setup(i => i.GetByPlatformTransactionIdAsync("Apple", "txn_123", It.IsAny<CancellationToken>())).ReturnsAsync(record);
        _mockUserRepo.Setup(u => u.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _webhookService.HandleRefundAsync("Apple", "txn_123", "User requested", CancellationToken.None);

        result.Should().BeTrue();
        user.Diamonds.Should().Be(50); // 150 - 100
        record.RefundedAt.Should().NotBeNull();
        record.RefundReason.Should().Be("User requested");

        _mockUserRepo.Verify(u => u.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _mockTransactionRepo.Verify(t => t.AddAsync(It.Is<PetTransaction>(pt => 
            pt.UserId == userId && pt.TransactionType == "IapRefund" && pt.DiamondsDelta == -100
        ), It.IsAny<CancellationToken>()), Times.Once);
        _mockIapRepo.Verify(i => i.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
