using Amora.Application.Abstractions;
using Amora.Application.Dtos.Messages;
using Amora.Application.Exceptions;
using Amora.Application.Pets;
using Amora.Application.Services;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Amora.Application.Tests.Services;

public class ChatServiceTests
{
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IMatchConnectionRepository> _mockMatchConnectionRepository;
    private readonly Mock<IChatMessageRepository> _mockChatMessageRepository;
    private readonly Mock<IRealtimeNotifier> _mockRealtimeNotifier;
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<PetFeatureGateService> _mockPetFeatureGateService;
    private readonly Mock<IChatReadStateRepository> _mockChatReadStateRepository;
    private readonly ChatService _chatService;

    // Fake repositories specifically for PetFeatureGateService dependencies
    private readonly Mock<IPetRepository> _mockPetRepository;
    private readonly Mock<IMatchMediaUsageRepository> _mockMatchMediaUsageRepository;

    public ChatServiceTests()
    {
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockMatchConnectionRepository = new Mock<IMatchConnectionRepository>();
        _mockChatMessageRepository = new Mock<IChatMessageRepository>();
        _mockRealtimeNotifier = new Mock<IRealtimeNotifier>();
        _mockMediator = new Mock<IMediator>();
        
        _mockPetRepository = new Mock<IPetRepository>();
        _mockMatchMediaUsageRepository = new Mock<IMatchMediaUsageRepository>();
        var petFeatureGateService = new PetFeatureGateService(_mockPetRepository.Object, _mockMatchMediaUsageRepository.Object);

        // We can pass the real PetFeatureGateService since it doesn't have an interface, 
        // but we'll mock its underlying dependencies instead to control its behavior.
        _mockChatReadStateRepository = new Mock<IChatReadStateRepository>();

        _chatService = new ChatService(
            _mockCurrentUserService.Object,
            _mockMatchConnectionRepository.Object,
            _mockChatMessageRepository.Object,
            _mockRealtimeNotifier.Object,
            _mockMediator.Object,
            petFeatureGateService, // Use real instance but with mocked dependencies
            _mockChatReadStateRepository.Object
        );
    }

    [Fact]
    public async Task SendMessageAsync_ShouldThrowForbidden_WhenUserIsNotParticipant()
    {
        // Arrange
        var matchId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(userId);
        _mockMatchConnectionRepository.Setup(m => m.IsParticipantAsync(matchId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = async () => await _chatService.SendMessageAsync(matchId, new SendMessageRequest { Type = "Text", Content = "Hello" });

        // Assert
        await act.Should().ThrowAsync<ForbiddenApiException>().WithMessage("You cannot send messages to this room.");
    }

    [Fact]
    public async Task SendMessageAsync_ShouldThrowValidationException_WhenMatchIsExpired()
    {
        // Arrange
        var matchId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(userId);
        _mockMatchConnectionRepository.Setup(m => m.IsParticipantAsync(matchId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
            
        // Match exists but is expired
        var match = new MatchConnection { Status = MatchStatus.Expired };
        _mockMatchConnectionRepository.Setup(m => m.GetByIdAsync(matchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        // Act
        var act = async () => await _chatService.SendMessageAsync(matchId, new SendMessageRequest { Type = "Text", Content = "Hello" });

        // Assert
        await act.Should().ThrowAsync<ValidationApiException>().WithMessage("Match đã hết hạn hoặc không còn hoạt động.");
    }

    [Fact]
    public async Task SendMessageAsync_ShouldThrowValidationException_WhenTypeIsInvalid()
    {
        // Arrange
        var matchId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(userId);
        _mockMatchConnectionRepository.Setup(m => m.IsParticipantAsync(matchId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
            
        var match = new MatchConnection { Status = MatchStatus.Active };
        _mockMatchConnectionRepository.Setup(m => m.GetByIdAsync(matchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        // Act
        var act = async () => await _chatService.SendMessageAsync(matchId, new SendMessageRequest { Type = "InvalidType" });

        // Assert
        await act.Should().ThrowAsync<ValidationApiException>().WithMessage("Unsupported message type.");
    }

    [Fact]
    public async Task SendMessageAsync_ShouldSucceed_AndNotifyRealtime_WhenValidTextMessage()
    {
        // Arrange
        var matchId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _mockCurrentUserService.Setup(c => c.UserId).Returns(userId);
        _mockMatchConnectionRepository.Setup(m => m.IsParticipantAsync(matchId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
            
        var match = new MatchConnection { Status = MatchStatus.Active };
        _mockMatchConnectionRepository.Setup(m => m.GetByIdAsync(matchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        // Act
        var result = await _chatService.SendMessageAsync(matchId, new SendMessageRequest { Type = "Text", Content = "Hello" });

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Sent");

        // Verify message was added to repository
        _mockChatMessageRepository.Verify(repo => repo.AddAsync(It.Is<ChatMessage>(m => 
            m.MatchId == matchId && 
            m.SenderId == userId && 
            m.Content == "Hello" && 
            m.MessageType == MessageType.Text
        ), It.IsAny<CancellationToken>()), Times.Once);

        // Verify match handshake was extended
        _mockMatchConnectionRepository.Verify(repo => repo.ExtendHandshakeAsync(matchId, It.IsAny<CancellationToken>()), Times.Once);

        // Verify realtime notification was sent
        _mockRealtimeNotifier.Verify(notifier => notifier.NotifyNewMessageAsync(It.IsAny<ChatMessage>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
