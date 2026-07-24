using Amora.Application.Abstractions;
using Amora.Application.Dtos.Posts;
using Amora.Application.Exceptions;
using Amora.Application.Services;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Amora.Application.Tests.Services;

public class VoicePostServiceTests
{
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IVoicePostRepository> _mockVoicePostRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IMessageBus> _mockMessageBus;
    private readonly Mock<IRealtimeNotifier> _mockRealtimeNotifier;
    private readonly IConfiguration _configuration;
    private readonly VoicePostService _voicePostService;

    public VoicePostServiceTests()
    {
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockVoicePostRepository = new Mock<IVoicePostRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockMessageBus = new Mock<IMessageBus>();
        _mockRealtimeNotifier = new Mock<IRealtimeNotifier>();

        var audioProcessingService = new AudioProcessingService(
            _mockVoicePostRepository.Object,
            _mockMessageBus.Object,
            _mockRealtimeNotifier.Object
        );

        var mockConfiguration = new Mock<IConfiguration>();
        mockConfiguration.Setup(c => c["Storage:BucketName"]).Returns("amora-test-bucket");
        _configuration = mockConfiguration.Object;

        _voicePostService = new VoicePostService(
            _mockCurrentUserService.Object,
            _mockVoicePostRepository.Object,
            _mockUserRepository.Object,
            null!,
            null!,
            audioProcessingService,
            _configuration,
            null!,
            NullLogger<VoicePostService>.Instance
        );
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowValidationException_WhenAudioUrlIsMissing()
    {
        // Act
        var act = async () => await _voicePostService.CreateAsync(new CreateVoicePostRequest { AudioUrl = "" });

        // Assert
        await act.Should().ThrowAsync<ValidationApiException>().WithMessage("AudioUrl is required.");
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowConflictException_WhenDailyLimitReached()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(userId);
        _mockVoicePostRepository.Setup(r => r.CountByPosterSinceAsync(userId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        // Act
        var act = async () => await _voicePostService.CreateAsync(new CreateVoicePostRequest { AudioUrl = "https://example.com/audio.m4a" });

        // Assert
        await act.Should().ThrowAsync<ConflictApiException>().WithMessage("You have reached the daily limit of 3 voice posts.");
    }

    [Fact]
    public async Task CreateAsync_ShouldCreatePostAndEnqueueProcessing_WhenValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(userId);
        _mockVoicePostRepository.Setup(r => r.CountByPosterSinceAsync(userId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _voicePostService.CreateAsync(new CreateVoicePostRequest { AudioUrl = "https://amora-test-bucket.s3.amazonaws.com/voices/abc.m4a" });

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(VoicePostStatus.Processing.ToString());
        result.PosterId.Should().Be(userId);

        _mockVoicePostRepository.Verify(r => r.AddAsync(It.Is<VoicePost>(p => 
            p.PosterId == userId && 
            p.AudioUrl == "https://amora-test-bucket.s3.amazonaws.com/voices/abc.m4a" &&
            p.Status == VoicePostStatus.Processing
        ), It.IsAny<CancellationToken>()), Times.Once);

        // Verify that AudioProcessingService published message with correct extracted key
        _mockMessageBus.Verify(m => m.PublishAsync(
            "tasks.process_voice_post",
            It.Is<object[]>(args => args.Length == 2 && args[1].ToString() == "voices/abc.m4a"),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task CloseAsync_ShouldThrowForbidden_WhenUserIsNotPoster()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(userId);
        _mockVoicePostRepository.Setup(r => r.GetByIdAsync(postId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VoicePost { Id = postId, PosterId = Guid.NewGuid() }); // Different poster

        // Act
        var act = async () => await _voicePostService.CloseAsync(postId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenApiException>().WithMessage("You are not allowed to close this post.");
    }

    [Fact]
    public async Task CloseAsync_ShouldUpdateStatusToClosed_WhenValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var post = new VoicePost { Id = postId, PosterId = userId, Status = VoicePostStatus.Open };
        _mockCurrentUserService.Setup(c => c.UserId).Returns(userId);
        _mockVoicePostRepository.Setup(r => r.GetByIdAsync(postId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        // Act
        await _voicePostService.CloseAsync(postId);

        // Assert
        post.Status.Should().Be(VoicePostStatus.Closed);
        _mockVoicePostRepository.Verify(r => r.UpdateAsync(post, It.IsAny<CancellationToken>()), Times.Once);
    }
}
