using Amora.Application.Exceptions;
using Amora.Application.Pets;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace Amora.Application.Tests.Pets;

public class PetFeatureGateServiceTests
{
    private readonly Mock<IPetRepository> _mockPetRepository;
    private readonly Mock<IMatchMediaUsageRepository> _mockMediaUsageRepository;
    private readonly PetFeatureGateService _petFeatureGateService;

    public PetFeatureGateServiceTests()
    {
        _mockPetRepository = new Mock<IPetRepository>();
        _mockMediaUsageRepository = new Mock<IMatchMediaUsageRepository>();

        _petFeatureGateService = new PetFeatureGateService(
            _mockPetRepository.Object,
            _mockMediaUsageRepository.Object
        );
    }

    [Fact]
    public async Task ValidateSendAsync_ShouldAllowTextVoiceAndSystem_WithoutCheckingPet()
    {
        // Arrange
        var matchId = Guid.NewGuid();

        // Act & Assert
        await _petFeatureGateService.Invoking(s => s.ValidateSendAsync(matchId, MessageType.Text, CancellationToken.None))
            .Should().NotThrowAsync();
        await _petFeatureGateService.Invoking(s => s.ValidateSendAsync(matchId, MessageType.Voice, CancellationToken.None))
            .Should().NotThrowAsync();
        await _petFeatureGateService.Invoking(s => s.ValidateSendAsync(matchId, MessageType.System, CancellationToken.None))
            .Should().NotThrowAsync();

        _mockPetRepository.Verify(p => p.GetByMatchIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ValidateSendAsync_ShouldThrowForbiddenException_WhenSendingImageAndPetIsEgg()
    {
        // Arrange
        var matchId = Guid.NewGuid();
        _mockPetRepository.Setup(p => p.GetByMatchIdAsync(matchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Pet { Stage = GrowthStage.ResonanceSeed });

        // Act
        var act = async () => await _petFeatureGateService.ValidateSendAsync(matchId, MessageType.Image, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenApiException>().WithMessage("Gửi ảnh mở khóa ở giai đoạn Mầm Non (RP ≥ 200).");
    }

    [Fact]
    public async Task ValidateSendAsync_ShouldAllowImage_WhenPetIsToddlerOrAbove()
    {
        // Arrange
        var matchId = Guid.NewGuid();
        _mockPetRepository.Setup(p => p.GetByMatchIdAsync(matchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Pet { Stage = GrowthStage.Sprout });

        // Act & Assert
        await _petFeatureGateService.Invoking(s => s.ValidateSendAsync(matchId, MessageType.Image, CancellationToken.None))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task RegisterImageSentAsync_ShouldThrowForbiddenException_WhenLimitExceeded()
    {
        // Arrange
        var matchId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _mockMediaUsageRepository.Setup(m => m.GetTodayAsync(matchId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MatchDailyMediaUsage { ImagesSent = 1 });

        // Act
        var act = async () => await _petFeatureGateService.RegisterImageSentAsync(matchId, userId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenApiException>().WithMessage("Đã dùng hết lượt gửi ảnh hôm nay (1 lần/ngày).");
    }

    [Fact]
    public async Task ValidateCallAsync_ShouldThrowForbiddenException_WhenVoiceCallAndPetIsTooYoung()
    {
        // Arrange
        var matchId = Guid.NewGuid();
        _mockPetRepository.Setup(p => p.GetByMatchIdAsync(matchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Pet { Stage = GrowthStage.Sprout }); // Needs Child

        // Act
        var act = async () => await _petFeatureGateService.ValidateCallAsync(matchId, "voice", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenApiException>().WithMessage("Voice call mở khóa ở giai đoạn Thú Nhỏ.");
    }
}
