using Amora.Application.Abstractions;
using Amora.Application.Dtos.Auth;
using Amora.Application.Exceptions;
using Amora.Application.Iap;
using Amora.Application.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Amora.Domain.Entities;
using Amora.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace Amora.Application.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IJwtTokenService> _mockJwtTokenService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockJwtTokenService = new Mock<IJwtTokenService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockCache = new Mock<IMemoryCache>();
        _mockConfig = new Mock<IConfiguration>();

        // DiamondRewardService is concrete but we mock its dependency
        var diamondRewardService = new DiamondRewardService(_mockUserRepository.Object);

        

        _authService = new AuthService(
            _mockUserRepository.Object,
            _mockJwtTokenService.Object,
            diamondRewardService
        );

        _mockJwtTokenService.Setup(j => j.CreateToken(It.IsAny<AppUser>()))
            .Returns(new AuthTokenResult { AccessToken = "test_token", ExpiresAt = DateTime.UtcNow.AddDays(1) });
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrowValidationException_WhenEmailOrPasswordMissing()
    {
        var act1 = async () => await _authService.RegisterAsync(new RegisterRequest { Email = "", Password = "123" }, CancellationToken.None);
        var act2 = async () => await _authService.RegisterAsync(new RegisterRequest { Email = "test@test.com", Password = "" }, CancellationToken.None);

        await act1.Should().ThrowAsync<ValidationApiException>().WithMessage("Email and password are required.");
        await act2.Should().ThrowAsync<ValidationApiException>().WithMessage("Email and password are required.");
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrowConflictException_WhenEmailAlreadyRegistered()
    {
        _mockUserRepository.Setup(u => u.GetByEmailAsync("test@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppUser());

        var act = async () => await _authService.RegisterAsync(new RegisterRequest { Email = "test@test.com", Password = "123" }, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictApiException>().WithMessage("Email already registered.");
    }

    [Fact]
    public async Task RegisterAsync_ShouldSucceed_AndReturnToken_WhenValid()
    {
        _mockUserRepository.Setup(u => u.GetByEmailAsync("test@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser)null!);

        var result = await _authService.RegisterAsync(new RegisterRequest { Email = "test@test.com", Password = "123", DisplayName = "Minh Khai" }, CancellationToken.None);

        result.Should().NotBeNull();
        result.AccessToken.Should().Be("test_token");
        result.DisplayName.Should().Be("Minh Khai");
        result.Diamonds.Should().Be(100);

        _mockUserRepository.Verify(u => u.AddAsync(It.Is<AppUser>(user => 
            user.Email == "test@test.com" && 
            user.DisplayName == "Minh Khai" &&
            user.Diamonds == 100
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowValidationException_WhenUserNotFound()
    {
        _mockUserRepository.Setup(u => u.GetByEmailForAuthAsync("test@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser)null!);

        var act = async () => await _authService.LoginAsync(new LoginRequest { Email = "test@test.com", Password = "123" }, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationApiException>().WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowValidationException_WhenPasswordIsIncorrect()
    {
        var user = new AppUser { Email = "test@test.com", PasswordHash = PasswordHasher.Hash("correct_password") };
        _mockUserRepository.Setup(u => u.GetByEmailForAuthAsync("test@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var act = async () => await _authService.LoginAsync(new LoginRequest { Email = "test@test.com", Password = "wrong_password" }, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationApiException>().WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task LoginAsync_ShouldSucceed_AndGrantDailyLoginBonus()
    {
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var user = new AppUser 
        { 
            Email = "test@test.com", 
            PasswordHash = PasswordHasher.Hash("correct_password"),
            Diamonds = 100,
            LastDiamondRewardDate = yesterday
        };
        _mockUserRepository.Setup(u => u.GetByEmailForAuthAsync("test@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _authService.LoginAsync(new LoginRequest { Email = "test@test.com", Password = "correct_password" }, CancellationToken.None);

        result.Should().NotBeNull();
        result.AccessToken.Should().Be("test_token");
        user.Diamonds.Should().Be(115); // +15 daily login bonus
        user.LastDiamondRewardDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));

        _mockUserRepository.Verify(u => u.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }
}
