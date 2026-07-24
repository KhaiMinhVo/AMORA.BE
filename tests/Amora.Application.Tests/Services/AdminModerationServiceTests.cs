using Amora.Application.Abstractions;
using Amora.Application.Dtos.Admin;
using Amora.Application.Exceptions;
using Amora.Application.Services;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace Amora.Application.Tests.Services;

public sealed class AdminModerationServiceTests
{
    private readonly Mock<IUserReportRepository> _reports = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUserBanRepository> _bans = new();
    private readonly Mock<IRealtimeNotifier> _realtime = new();
    private readonly AdminModerationService _service;

    public AdminModerationServiceTests()
    {
        _service = new AdminModerationService(
            _reports.Object,
            _users.Object,
            _bans.Object,
            _realtime.Object,
            null!,
            null!,
            null!,
            new TrustScoreService(_users.Object, _bans.Object));
    }

    [Fact]
    public async Task BanUserAsync_RejectsAdmin()
    {
        var id = Guid.NewGuid();
        _users.Setup(x => x.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppUser { Id = id, Role = "Admin" });

        var act = () => _service.BanUserAsync(id, new BanUserRequest());
        await act.Should().ThrowAsync<ValidationApiException>();
    }

    [Fact]
    public async Task BanUserAsync_IsIdempotentForAlreadyBannedUser()
    {
        var id = Guid.NewGuid();
        _users.Setup(x => x.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppUser { Id = id, Role = "User", IsBanned = true });

        await _service.BanUserAsync(id, new BanUserRequest { Reason = "duplicate" });

        _bans.Verify(x => x.AddAsync(It.IsAny<UserBan>(), It.IsAny<CancellationToken>()), Times.Never);
        _realtime.Verify(x => x.DisconnectUserAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveReportAsync_RejectsConcurrentClaim()
    {
        var report = new UserReport { Id = Guid.NewGuid(), Status = ReportStatus.Pending };
        _reports.Setup(x => x.GetByIdAsync(report.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);
        _reports.Setup(x => x.TryTransitionStatusAsync(
                report.Id,
                ReportStatus.Pending,
                ReportStatus.Processing,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = () => _service.ResolveReportAsync(
            report.Id,
            new ResolveReportRequest { Action = "Warning" });

        await act.Should().ThrowAsync<ConflictApiException>();
    }
}
