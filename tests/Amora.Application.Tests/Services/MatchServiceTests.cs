using Amora.Application.Abstractions;
using Amora.Application.Dtos.Matches;
using Amora.Application.Exceptions;
using Amora.Application.Services;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;
using Amora.Domain.Results;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Amora.Application.Tests.Services;

public class MatchServiceTests
{
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IVoicePostRepository> _mockVoicePostRepo;
    private readonly Mock<IVoiceCommentRepository> _mockVoiceCommentRepo;
    private readonly Mock<IMatchConnectionRepository> _mockMatchRepo;
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IChatMessageRepository> _mockChatMessageRepo;
    private readonly Mock<IRealtimeNotifier> _mockRealtimeNotifier;
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<IPetRepository> _mockPetRepo;
    private readonly Mock<IChatReadStateRepository> _mockReadStateRepo;
    private readonly MatchService _matchService;

    public MatchServiceTests()
    {
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockVoicePostRepo = new Mock<IVoicePostRepository>();
        _mockVoiceCommentRepo = new Mock<IVoiceCommentRepository>();
        _mockMatchRepo = new Mock<IMatchConnectionRepository>();
        _mockUserRepo = new Mock<IUserRepository>();
        _mockChatMessageRepo = new Mock<IChatMessageRepository>();
        _mockRealtimeNotifier = new Mock<IRealtimeNotifier>();
        _mockMediator = new Mock<IMediator>();
        _mockPetRepo = new Mock<IPetRepository>();
        _mockReadStateRepo = new Mock<IChatReadStateRepository>();

        _matchService = new MatchService(
            _mockCurrentUserService.Object,
            _mockVoicePostRepo.Object,
            _mockVoiceCommentRepo.Object,
            _mockMatchRepo.Object,
            _mockUserRepo.Object,
            _mockChatMessageRepo.Object,
            _mockRealtimeNotifier.Object,
            _mockMediator.Object,
            _mockPetRepo.Object,
            _mockReadStateRepo.Object,
            null!,
            null!,
            null!,
            null!,
            null!
        );
    }

    [Fact]
    public async Task CreateMatchAsync_ShouldThrowNotFound_WhenPostNotFound()
    {
        // Arrange
        _mockVoicePostRepo.Setup(p => p.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VoicePost)null!);

        // Act
        var act = async () => await _matchService.CreateMatchAsync(new CreateMatchRequest { PostId = Guid.NewGuid(), CommentId = Guid.NewGuid() });

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>().WithMessage("Voice post not found.");
    }

    [Fact]
    public async Task CreateMatchAsync_ShouldThrowForbidden_WhenUserIsNotPoster()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var post = new VoicePost { Id = postId, PosterId = Guid.NewGuid() }; // Someone else's post
        _mockCurrentUserService.Setup(c => c.UserId).Returns(userId);
        _mockVoicePostRepo.Setup(p => p.GetByIdAsync(postId, It.IsAny<CancellationToken>())).ReturnsAsync(post);

        // Act
        var act = async () => await _matchService.CreateMatchAsync(new CreateMatchRequest { PostId = postId, CommentId = Guid.NewGuid() });

        // Assert
        await act.Should().ThrowAsync<ForbiddenApiException>().WithMessage("You are not allowed to match on this post.");
    }

    [Fact]
    public async Task CreateMatchAsync_ShouldCreateConnection_WhenValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var commentId = Guid.NewGuid();
        
        var post = new VoicePost { Id = postId, PosterId = userId };
        var comment = new VoiceComment { Id = commentId, PostId = postId, Status = VoiceCommentStatus.Pending, CommenterId = Guid.NewGuid() };
        
        _mockCurrentUserService.Setup(c => c.UserId).Returns(userId);
        _mockVoicePostRepo.Setup(p => p.GetByIdAsync(postId, It.IsAny<CancellationToken>())).ReturnsAsync(post);
        _mockVoiceCommentRepo.Setup(c => c.GetByIdAsync(commentId, It.IsAny<CancellationToken>())).ReturnsAsync(comment);

        var match = new MatchConnection { Id = Guid.NewGuid(), UserAId = post.PosterId, UserBId = comment.CommenterId, Status = MatchStatus.Active };
        var tupleResult = (MatchConnection: match, PostClosed: true);
        
        _mockMatchRepo.Setup(m => m.CreateConnectionAsync(postId, commentId, userId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tupleResult);

        // Act
        var response = await _matchService.CreateMatchAsync(new CreateMatchRequest { PostId = postId, CommentId = commentId });

        // Assert
        response.Should().NotBeNull();
        response.MatchId.Should().Be(match.Id);

        _mockChatMessageRepo.Verify(r => r.AddAsync(It.Is<ChatMessage>(m => m.MessageType == MessageType.System), It.IsAny<CancellationToken>()), Times.Once);
        _mockRealtimeNotifier.Verify(n => n.NotifyMatchCreatedAsync(match, It.IsAny<CancellationToken>()), Times.Once);
    }
}
