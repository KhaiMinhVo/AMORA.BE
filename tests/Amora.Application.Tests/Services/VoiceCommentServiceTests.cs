using Amora.Application.Abstractions;
using Amora.Application.Dtos.Comments;
using Amora.Application.Exceptions;
using Amora.Application.Services;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Amora.Application.Tests.Services;

public class VoiceCommentServiceTests
{
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IVoicePostRepository> _mockVoicePostRepo;
    private readonly Mock<IVoiceCommentRepository> _mockVoiceCommentRepo;
    private readonly VoiceCommentService _voiceCommentService;

    public VoiceCommentServiceTests()
    {
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockVoicePostRepo = new Mock<IVoicePostRepository>();
        _mockVoiceCommentRepo = new Mock<IVoiceCommentRepository>();

        _voiceCommentService = new VoiceCommentService(
            _mockCurrentUserService.Object,
            _mockVoicePostRepo.Object,
            _mockVoiceCommentRepo.Object,
            null!,
            null!,
            null!,
            NullLogger<VoiceCommentService>.Instance
        );
    }

    [Fact]
    public async Task CreateCommentAsync_ShouldThrowValidationException_WhenAudioUrlMissing()
    {
        var act = async () => await _voiceCommentService.CreateCommentAsync(Guid.NewGuid(), new CreateVoiceCommentRequest { AudioUrl = "" });
        await act.Should().ThrowAsync<ValidationApiException>().WithMessage("AudioUrl is required.");
    }

    [Fact]
    public async Task CreateCommentAsync_ShouldThrowValidationException_WhenDurationInvalid()
    {
        var act = async () => await _voiceCommentService.CreateCommentAsync(Guid.NewGuid(), new CreateVoiceCommentRequest { AudioUrl = "url", Duration = 0 });
        await act.Should().ThrowAsync<ValidationApiException>().WithMessage("Duration must be greater than zero.");
    }

    [Fact]
    public async Task CreateCommentAsync_ShouldThrowNotFound_WhenPostNotFound()
    {
        _mockVoicePostRepo.Setup(p => p.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VoicePost)null!);

        var act = async () => await _voiceCommentService.CreateCommentAsync(Guid.NewGuid(), new CreateVoiceCommentRequest { AudioUrl = "url", Duration = 10 });
        await act.Should().ThrowAsync<NotFoundApiException>().WithMessage("Voice post not found.");
    }

    [Fact]
    public async Task CreateCommentAsync_ShouldThrowConflict_WhenPostIsClosed()
    {
        var post = new VoicePost { Id = Guid.NewGuid(), Status = VoicePostStatus.Closed };
        _mockVoicePostRepo.Setup(p => p.GetByIdAsync(post.Id, It.IsAny<CancellationToken>())).ReturnsAsync(post);

        var act = async () => await _voiceCommentService.CreateCommentAsync(post.Id, new CreateVoiceCommentRequest { AudioUrl = "url", Duration = 10 });
        await act.Should().ThrowAsync<ConflictApiException>().WithMessage("This voice post is already closed.");
    }

    [Fact]
    public async Task CreateCommentAsync_ShouldThrowConflict_WhenUserAlreadyCommented()
    {
        var userId = Guid.NewGuid();
        var post = new VoicePost { Id = Guid.NewGuid(), Status = VoicePostStatus.Open };
        _mockCurrentUserService.Setup(c => c.UserId).Returns(userId);
        _mockVoicePostRepo.Setup(p => p.GetByIdAsync(post.Id, It.IsAny<CancellationToken>())).ReturnsAsync(post);
        _mockVoiceCommentRepo.Setup(c => c.HasUserCommentedOnPostAsync(userId, post.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = async () => await _voiceCommentService.CreateCommentAsync(post.Id, new CreateVoiceCommentRequest { AudioUrl = "url", Duration = 10 });
        await act.Should().ThrowAsync<ConflictApiException>().WithMessage("You have already commented on this post.");
    }

    [Fact]
    public async Task CreateCommentAsync_ShouldSucceed_WhenValid()
    {
        var userId = Guid.NewGuid();
        var post = new VoicePost { Id = Guid.NewGuid(), Status = VoicePostStatus.Open };
        _mockCurrentUserService.Setup(c => c.UserId).Returns(userId);
        _mockVoicePostRepo.Setup(p => p.GetByIdAsync(post.Id, It.IsAny<CancellationToken>())).ReturnsAsync(post);
        _mockVoiceCommentRepo.Setup(c => c.HasUserCommentedOnPostAsync(userId, post.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _voiceCommentService.CreateCommentAsync(post.Id, new CreateVoiceCommentRequest { AudioUrl = "url", Duration = 10 });

        result.Should().NotBeNull();
        result.Status.Should().Be(VoiceCommentStatus.Pending.ToString());
        
        _mockVoiceCommentRepo.Verify(r => r.AddAsync(It.Is<VoiceComment>(c => 
            c.PostId == post.Id && 
            c.CommenterId == userId && 
            c.AudioUrl == "url" && 
            c.Duration == 10
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCommentsAsync_ShouldThrowForbidden_WhenUserIsNotPoster()
    {
        var userId = Guid.NewGuid();
        var post = new VoicePost { Id = Guid.NewGuid(), PosterId = Guid.NewGuid() }; // Different poster
        _mockCurrentUserService.Setup(c => c.UserId).Returns(userId);
        _mockVoicePostRepo.Setup(p => p.GetByIdAsync(post.Id, It.IsAny<CancellationToken>())).ReturnsAsync(post);

        var act = async () => await _voiceCommentService.GetCommentsAsync(post.Id, 1, 10);
        await act.Should().ThrowAsync<ForbiddenApiException>().WithMessage("You are not allowed to view this private queue.");
    }

    [Fact]
    public async Task GetCommentsAsync_ShouldReturnPagedComments_WhenValid()
    {
        var userId = Guid.NewGuid();
        var post = new VoicePost { Id = Guid.NewGuid(), PosterId = userId };
        _mockCurrentUserService.Setup(c => c.UserId).Returns(userId);
        _mockVoicePostRepo.Setup(p => p.GetByIdAsync(post.Id, It.IsAny<CancellationToken>())).ReturnsAsync(post);
        
        var comments = new List<VoiceComment>
        {
            new VoiceComment { Id = Guid.NewGuid(), CommenterId = Guid.NewGuid(), AudioUrl = "url1", Status = VoiceCommentStatus.Pending },
            new VoiceComment { Id = Guid.NewGuid(), CommenterId = Guid.NewGuid(), AudioUrl = "url2", Status = VoiceCommentStatus.Pending }
        };
        _mockVoiceCommentRepo.Setup(c => c.GetPagedByPostIdAsync(post.Id, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((comments, 2));

        var result = await _voiceCommentService.GetCommentsAsync(post.Id, 1, 10);

        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items[0].AudioUrl.Should().Be("url1");
    }
}
