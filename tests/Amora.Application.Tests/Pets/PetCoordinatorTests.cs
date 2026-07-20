using System.Text.Json;
using Amora.Application.Abstractions;
using Amora.Application.Messaging;
using Amora.Application.Pets;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace Amora.Application.Tests.Pets;

public class PetCoordinatorTests
{
    private readonly Mock<IPetRepository> _mockPetRepo;
    private readonly Mock<IMatchConnectionRepository> _mockMatchRepo;
    private readonly Mock<IMessagePublisher> _mockPublisher;
    private readonly Mock<IPetRealtimeNotifier> _mockNotifier;
    private readonly PetCoordinator _coordinator;

    public PetCoordinatorTests()
    {
        _mockPetRepo = new Mock<IPetRepository>();
        _mockMatchRepo = new Mock<IMatchConnectionRepository>();
        _mockPublisher = new Mock<IMessagePublisher>();
        _mockNotifier = new Mock<IPetRealtimeNotifier>();

        _coordinator = new PetCoordinator(
            _mockPetRepo.Object,
            _mockMatchRepo.Object,
            _mockPublisher.Object,
            _mockNotifier.Object
        );
    }

    [Fact]
    public async Task CreateForMatchAsync_ShouldReturnExisting_WhenPetAlreadyExists()
    {
        var matchId = Guid.NewGuid();
        var existingPet = new Pet { MatchId = matchId };
        _mockPetRepo.Setup(p => p.GetByMatchIdAsync(matchId, It.IsAny<CancellationToken>())).ReturnsAsync(existingPet);

        var result = await _coordinator.CreateForMatchAsync(matchId, CancellationToken.None);

        result.Should().Be(existingPet);
        _mockPetRepo.Verify(p => p.AddAsync(It.IsAny<Pet>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateForMatchAsync_ShouldCreateAndNotify_WhenNew()
    {
        var matchId = Guid.NewGuid();
        _mockPetRepo.Setup(p => p.GetByMatchIdAsync(matchId, It.IsAny<CancellationToken>())).ReturnsAsync((Pet)null!);
        _mockMatchRepo.Setup(m => m.GetByIdAsync(matchId, It.IsAny<CancellationToken>())).ReturnsAsync(new MatchConnection());

        var result = await _coordinator.CreateForMatchAsync(matchId, CancellationToken.None);

        result.Should().NotBeNull();
        result.MatchId.Should().Be(matchId);
        result.Stage.Should().Be(GrowthStage.ResonanceSeed);
        result.Hp.Should().Be(80);

        _mockPetRepo.Verify(p => p.AddAsync(It.IsAny<Pet>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockPetRepo.Verify(p => p.AddHistoryAsync(It.Is<PetStateHistory>(h => h.EventType == "Created"), It.IsAny<CancellationToken>()), Times.Once);
        _mockNotifier.Verify(n => n.NotifyPetStatusUpdatedAsync(It.IsAny<Pet>(), It.IsAny<MatchConnection>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessTextMessageAsync_ShouldUpdatePetAndNotify_WhenValid()
    {
        var matchId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var pet = new Pet { Id = Guid.NewGuid(), MatchId = matchId, Hp = 50, Rp = 10, Stage = GrowthStage.ResonanceSeed, IsFrozen = false };

        _mockPetRepo.Setup(p => p.GetByMatchIdAsync(matchId, It.IsAny<CancellationToken>())).ReturnsAsync(pet);

        await _coordinator.ProcessTextMessageAsync(matchId, senderId, CancellationToken.None);

        pet.Hp.Should().BeGreaterThan(50);
        pet.Rp.Should().BeGreaterThan(10);
        
        _mockPetRepo.Verify(p => p.AddHistoryAsync(It.Is<PetStateHistory>(h => h.EventType == "TextInteraction"), It.IsAny<CancellationToken>()), Times.Once);
        _mockPetRepo.Verify(p => p.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishVoiceForVibeAsync_ShouldPublishMessage()
    {
        var matchId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await _coordinator.PublishVoiceForVibeAsync(matchId, userId, "audio.url", 15.5, CancellationToken.None);

        _mockPublisher.Verify(p => p.PublishAsync("chat_voice_processed", It.Is<ChatVoiceProcessedMessage>(m => 
            m.MatchId == matchId && m.UserId == userId && m.AudioUrl == "audio.url" && m.DurationSeconds == 15.5
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EvaluateChronotypeAsync_MorningMost_ShouldReturnDog()
    {
        var matchId = Guid.NewGuid();
        _mockMatchRepo.Setup(m => m.GetByIdAsync(matchId, It.IsAny<CancellationToken>())).ReturnsAsync(new MatchConnection());
        var msgs = new List<ChatMessage>
        {
            new ChatMessage { MessageType = MessageType.Voice, CreatedAt = new DateTimeOffset(2023, 1, 2, 0, 0, 0, TimeSpan.Zero) }, // 07:00 local (Monday) -> Morning
            new ChatMessage { MessageType = MessageType.Voice, CreatedAt = new DateTimeOffset(2023, 1, 2, 1, 0, 0, TimeSpan.Zero) }, // 08:00 local (Monday) -> Morning
            new ChatMessage { MessageType = MessageType.Voice, CreatedAt = new DateTimeOffset(2023, 1, 2, 16, 0, 0, TimeSpan.Zero) } // 23:00 local (Monday) -> Night
        };
        _mockPetRepo.Setup(p => p.GetByMatchIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Pet { MatchId = matchId });
        var mockChatRepo = new Mock<IChatMessageRepository>();
        var page = ((IReadOnlyList<ChatMessage>)msgs, (string)null!);
        mockChatRepo.Setup(c => c.GetByMatchAsync(matchId, null, 500, It.IsAny<CancellationToken>())).ReturnsAsync(page);

        var coordinator = new PetCoordinator(_mockPetRepo.Object, _mockMatchRepo.Object, _mockPublisher.Object, _mockNotifier.Object, mockChatRepo.Object, null!, null!);
        var result = await coordinator.EvaluateChronotypeAsync(matchId, CancellationToken.None);

        result.Should().Be(PetType.Dog);
    }

    [Fact]
    public async Task EvaluateChronotypeAsync_NightMost_ShouldReturnCat()
    {
        var matchId = Guid.NewGuid();
        _mockMatchRepo.Setup(m => m.GetByIdAsync(matchId, It.IsAny<CancellationToken>())).ReturnsAsync(new MatchConnection());
        var msgs = new List<ChatMessage>
        {
            new ChatMessage { MessageType = MessageType.Voice, CreatedAt = new DateTimeOffset(2023, 1, 2, 0, 0, 0, TimeSpan.Zero) }, // 07:00 local (Monday) -> Morning
            new ChatMessage { MessageType = MessageType.Voice, CreatedAt = new DateTimeOffset(2023, 1, 2, 16, 0, 0, TimeSpan.Zero) }, // 23:00 local (Monday) -> Night
            new ChatMessage { MessageType = MessageType.Voice, CreatedAt = new DateTimeOffset(2023, 1, 2, 17, 0, 0, TimeSpan.Zero) } // 00:00 local (Tuesday) -> Night
        };
        var mockChatRepo = new Mock<IChatMessageRepository>();
        var page = ((IReadOnlyList<ChatMessage>)msgs, (string)null!);
        mockChatRepo.Setup(c => c.GetByMatchAsync(matchId, null, 500, It.IsAny<CancellationToken>())).ReturnsAsync(page);

        var coordinator = new PetCoordinator(_mockPetRepo.Object, _mockMatchRepo.Object, _mockPublisher.Object, _mockNotifier.Object, mockChatRepo.Object, null!, null!);
        var result = await coordinator.EvaluateChronotypeAsync(matchId, CancellationToken.None);

        result.Should().Be(PetType.Cat);
    }

    [Fact]
    public async Task EvaluateChronotypeAsync_DaytimeMost_ShouldReturnRabbit()
    {
        var matchId = Guid.NewGuid();
        _mockMatchRepo.Setup(m => m.GetByIdAsync(matchId, It.IsAny<CancellationToken>())).ReturnsAsync(new MatchConnection());
        var msgs = new List<ChatMessage>
        {
            new ChatMessage { MessageType = MessageType.Voice, CreatedAt = new DateTimeOffset(2023, 1, 2, 0, 0, 0, TimeSpan.Zero) }, // 07:00 local -> Morning
            new ChatMessage { MessageType = MessageType.Voice, CreatedAt = new DateTimeOffset(2023, 1, 2, 6, 0, 0, TimeSpan.Zero) }, // 13:00 local -> Daytime
            new ChatMessage { MessageType = MessageType.Voice, CreatedAt = new DateTimeOffset(2023, 1, 2, 8, 0, 0, TimeSpan.Zero) }  // 15:00 local -> Daytime
        };
        var mockChatRepo = new Mock<IChatMessageRepository>();
        mockChatRepo.Setup(c => c.GetByMatchAsync(matchId, null, 500, It.IsAny<CancellationToken>())).ReturnsAsync(((IReadOnlyList<ChatMessage>)msgs, (string)null!));

        var coordinator = new PetCoordinator(_mockPetRepo.Object, _mockMatchRepo.Object, _mockPublisher.Object, _mockNotifier.Object, mockChatRepo.Object, null!, null!);
        var result = await coordinator.EvaluateChronotypeAsync(matchId, CancellationToken.None);

        result.Should().Be(PetType.Rabbit);
    }

    [Fact]
    public async Task EvaluateChronotypeAsync_WeekendMost_ShouldReturnOtter()
    {
        var matchId = Guid.NewGuid();
        _mockMatchRepo.Setup(m => m.GetByIdAsync(matchId, It.IsAny<CancellationToken>())).ReturnsAsync(new MatchConnection());
        var msgs = new List<ChatMessage>
        {
            new ChatMessage { MessageType = MessageType.Voice, CreatedAt = new DateTimeOffset(2023, 1, 2, 6, 0, 0, TimeSpan.Zero) }, // Monday 13:00 local -> Daytime
            new ChatMessage { MessageType = MessageType.Voice, CreatedAt = new DateTimeOffset(2023, 1, 7, 0, 0, 0, TimeSpan.Zero) }, // Saturday 07:00 local -> Weekend
            new ChatMessage { MessageType = MessageType.Voice, CreatedAt = new DateTimeOffset(2023, 1, 8, 0, 0, 0, TimeSpan.Zero) }  // Sunday 07:00 local -> Weekend
        };
        var mockChatRepo = new Mock<IChatMessageRepository>();
        mockChatRepo.Setup(c => c.GetByMatchAsync(matchId, null, 500, It.IsAny<CancellationToken>())).ReturnsAsync(((IReadOnlyList<ChatMessage>)msgs, (string)null!));

        var coordinator = new PetCoordinator(_mockPetRepo.Object, _mockMatchRepo.Object, _mockPublisher.Object, _mockNotifier.Object, mockChatRepo.Object, null!, null!);
        var result = await coordinator.EvaluateChronotypeAsync(matchId, CancellationToken.None);

        result.Should().Be(PetType.Otter);
    }

    [Fact]
    public async Task EvaluateChronotypeAsync_TieBreaker_ShouldBeDeterministic()
    {
        var matchId = Guid.NewGuid();
        _mockMatchRepo.Setup(m => m.GetByIdAsync(matchId, It.IsAny<CancellationToken>())).ReturnsAsync(new MatchConnection());
        var msgs = new List<ChatMessage>
        {
            new ChatMessage { MessageType = MessageType.Voice, CreatedAt = new DateTimeOffset(2023, 1, 2, 0, 0, 0, TimeSpan.Zero) }, // Morning
            new ChatMessage { MessageType = MessageType.Voice, CreatedAt = new DateTimeOffset(2023, 1, 2, 16, 0, 0, TimeSpan.Zero) } // Night
        };
        var mockChatRepo = new Mock<IChatMessageRepository>();
        mockChatRepo.Setup(c => c.GetByMatchAsync(matchId, null, 500, It.IsAny<CancellationToken>())).ReturnsAsync(((IReadOnlyList<ChatMessage>)msgs, (string)null!));

        var coordinator = new PetCoordinator(_mockPetRepo.Object, _mockMatchRepo.Object, _mockPublisher.Object, _mockNotifier.Object, mockChatRepo.Object, null!, null!);
        var result = await coordinator.EvaluateChronotypeAsync(matchId, CancellationToken.None);

        // Tie breaker is Morning(Dog) > Night(Cat) > Otter > Rabbit
        // Since Morning and Night are both 1, it should pick Morning (Dog).
        result.Should().Be(PetType.Dog);
    }

    [Fact]
    public async Task EvaluateChronotypeAsync_NoVoice_ShouldBeDeterministicFallback()
    {
        var matchId = Guid.NewGuid();
        _mockMatchRepo.Setup(m => m.GetByIdAsync(matchId, It.IsAny<CancellationToken>())).ReturnsAsync(new MatchConnection());
        var mockChatRepo = new Mock<IChatMessageRepository>();
        mockChatRepo.Setup(c => c.GetByMatchAsync(matchId, null, 500, It.IsAny<CancellationToken>())).ReturnsAsync(((IReadOnlyList<ChatMessage>)new List<ChatMessage>(), (string)null!));

        var coordinator = new PetCoordinator(_mockPetRepo.Object, _mockMatchRepo.Object, _mockPublisher.Object, _mockNotifier.Object, mockChatRepo.Object, null!, null!);
        var result = await coordinator.EvaluateChronotypeAsync(matchId, CancellationToken.None);

        var hash = Math.Abs(matchId.GetHashCode());
        var expectedType = (PetType)((hash % 4) + 1);

        result.Should().Be(expectedType);
    }
}
