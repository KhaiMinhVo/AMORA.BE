using Amora.Application.Abstractions;
using Amora.Application.Dtos.Safety;
using Amora.Application.Exceptions;
using Amora.Application.Services;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Amora.Application.Tests.Services;

public class TrustSafetyServiceTests
{
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IUserReportRepository> _mockReportRepository;
    private readonly Mock<IUserBlockRepository> _mockBlockRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly TrustSafetyService _trustSafetyService;

    public TrustSafetyServiceTests()
    {
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockReportRepository = new Mock<IUserReportRepository>();
        _mockBlockRepository = new Mock<IUserBlockRepository>();
        _mockUserRepository = new Mock<IUserRepository>();

        _trustSafetyService = new TrustSafetyService(
            _mockCurrentUserService.Object,
            _mockReportRepository.Object,
            _mockBlockRepository.Object,
            _mockUserRepository.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            NullLogger<TrustSafetyService>.Instance
        );
    }

    [Fact]
    public async Task ReportUserAsync_ShouldThrowValidationException_WhenReportingSelf()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(userId);
        
        // Act
        var act = async () => await _trustSafetyService.ReportUserAsync(userId, new CreateReportRequest { Reason = "Harassment" });

        // Assert
        await act.Should().ThrowAsync<ValidationApiException>().WithMessage("You cannot report yourself.");
    }

    [Fact]
    public async Task ReportUserAsync_ShouldThrowNotFoundException_WhenTargetUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(userId);
        _mockUserRepository.Setup(u => u.GetByIdAsync(targetId, It.IsAny<CancellationToken>())).ReturnsAsync((AppUser)null!);

        // Act
        var act = async () => await _trustSafetyService.ReportUserAsync(targetId, new CreateReportRequest { Reason = "Harassment" });

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>().WithMessage("User not found.");
    }

    [Fact]
    public async Task ReportUserAsync_ShouldThrowConflictException_WhenAlreadyReported()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(userId);
        _mockUserRepository.Setup(u => u.GetByIdAsync(targetId, It.IsAny<CancellationToken>())).ReturnsAsync(new AppUser());
        _mockReportRepository.Setup(r => r.ExistsRecentAsync(userId, targetId, null, null, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var act = async () => await _trustSafetyService.ReportUserAsync(targetId, new CreateReportRequest { Reason = "Harassment" });

        // Assert
        await act.Should().ThrowAsync<ConflictApiException>().WithMessage("You have already reported this user.");
    }

    [Fact]
    public async Task ReportUserAsync_ShouldCreateReport_WhenValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(userId);
        _mockUserRepository.Setup(u => u.GetByIdAsync(targetId, It.IsAny<CancellationToken>())).ReturnsAsync(new AppUser());
        _mockReportRepository.Setup(r => r.ExistsRecentAsync(userId, targetId, null, null, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await _trustSafetyService.ReportUserAsync(targetId, new CreateReportRequest { Reason = "Harassment", Description = "Testing" });

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(ReportStatus.Pending.ToString());

        _mockReportRepository.Verify(r => r.AddAsync(It.Is<UserReport>(ur => 
            ur.ReporterId == userId && 
            ur.TargetUserId == targetId && 
            ur.Reason == ReportReason.Harassment &&
            ur.Description == "Testing" &&
            ur.Status == ReportStatus.Pending
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BlockUserAsync_ShouldThrowValidationException_WhenBlockingSelf()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(userId);

        // Act
        var act = async () => await _trustSafetyService.BlockUserAsync(userId);

        // Assert
        await act.Should().ThrowAsync<ValidationApiException>().WithMessage("You cannot block yourself.");
    }

    [Fact]
    public async Task BlockUserAsync_ShouldSucceed_WhenValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(userId);
        _mockUserRepository.Setup(u => u.GetByIdAsync(targetId, It.IsAny<CancellationToken>())).ReturnsAsync(new AppUser());
        _mockBlockRepository.Setup(b => b.IsBlockedAsync(userId, targetId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await _trustSafetyService.BlockUserAsync(targetId);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Blocked");

        _mockBlockRepository.Verify(b => b.AddAsync(It.Is<UserBlock>(ub => 
            ub.BlockerId == userId && 
            ub.BlockedUserId == targetId
        ), It.IsAny<CancellationToken>()), Times.Once);
    }
}
