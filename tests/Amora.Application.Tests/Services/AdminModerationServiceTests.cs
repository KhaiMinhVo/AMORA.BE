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

public class AdminModerationServiceTests
{
    private readonly Mock<IUserReportRepository> _mockReportRepo;
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IRealtimeNotifier> _mockRealtimeNotifier;
    private readonly AdminModerationService _adminService;

    public AdminModerationServiceTests()
    {
        _mockReportRepo = new Mock<IUserReportRepository>();
        _mockUserRepo = new Mock<IUserRepository>();
        _mockRealtimeNotifier = new Mock<IRealtimeNotifier>();

        _adminService = new AdminModerationService(
            _mockReportRepo.Object,
            _mockUserRepo.Object,
            _mockRealtimeNotifier.Object
        );
    }

    [Fact]
    public async Task BanUserAsync_ShouldThrowValidationException_WhenBanningAdmin()
    {
        var adminId = Guid.NewGuid();
        _mockUserRepo.Setup(u => u.GetByIdForUpdateAsync(adminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppUser { Id = adminId, Role = "Admin" });

        var act = async () => await _adminService.BanUserAsync(adminId, new BanUserRequest { Reason = "Spam" }, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationApiException>().WithMessage("Cannot ban an admin.");
    }

    [Fact]
    public async Task BanUserAsync_ShouldBanUserAndDisconnectSignalR_WhenValid()
    {
        var targetId = Guid.NewGuid();
        var user = new AppUser { Id = targetId, Role = "User", IsBanned = false };
        _mockUserRepo.Setup(u => u.GetByIdForUpdateAsync(targetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await _adminService.BanUserAsync(targetId, new BanUserRequest { Reason = "Spam", DurationDays = 7 }, CancellationToken.None);

        user.IsBanned.Should().BeTrue();
        user.BanReason.Should().Be("Spam");
        user.BannedUntil.Should().NotBeNull();
        user.BannedUntil.Value.Should().BeCloseTo(DateTimeOffset.UtcNow.AddDays(7), TimeSpan.FromSeconds(5));

        _mockUserRepo.Verify(u => u.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _mockRealtimeNotifier.Verify(n => n.DisconnectUserAsync(targetId, "Spam", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveReportAsync_ShouldThrowValidationException_WhenReportAlreadyResolved()
    {
        var reportId = Guid.NewGuid();
        _mockReportRepo.Setup(r => r.GetByIdAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserReport { Id = reportId, Status = ReportStatus.ActionTaken });

        var act = async () => await _adminService.ResolveReportAsync(reportId, new ResolveReportRequest { Action = "Ban" }, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationApiException>().WithMessage("Report is already resolved.");
    }

    [Fact]
    public async Task ResolveReportAsync_ShouldBanTargetUser_WhenActionIsBan()
    {
        var reportId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var report = new UserReport { Id = reportId, TargetUserId = targetId, Status = ReportStatus.Pending };
        var targetUser = new AppUser { Id = targetId, Role = "User" };

        _mockReportRepo.Setup(r => r.GetByIdAsync(reportId, It.IsAny<CancellationToken>())).ReturnsAsync(report);
        _mockUserRepo.Setup(u => u.GetByIdForUpdateAsync(targetId, It.IsAny<CancellationToken>())).ReturnsAsync(targetUser);

        await _adminService.ResolveReportAsync(reportId, new ResolveReportRequest { Action = "Ban", BanDurationDays = 3, ResolutionNote = "Banned via report" }, CancellationToken.None);

        report.Status.Should().Be(ReportStatus.ActionTaken);
        _mockReportRepo.Verify(r => r.UpdateAsync(report, It.IsAny<CancellationToken>()), Times.Once);

        targetUser.IsBanned.Should().BeTrue();
        targetUser.BanReason.Should().Be("Banned via report");
        _mockUserRepo.Verify(u => u.UpdateAsync(targetUser, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveReportAsync_ShouldDismissReport_WhenActionIsIgnore()
    {
        var reportId = Guid.NewGuid();
        var report = new UserReport { Id = reportId, Status = ReportStatus.Pending };

        _mockReportRepo.Setup(r => r.GetByIdAsync(reportId, It.IsAny<CancellationToken>())).ReturnsAsync(report);

        await _adminService.ResolveReportAsync(reportId, new ResolveReportRequest { Action = "Ignore" }, CancellationToken.None);

        report.Status.Should().Be(ReportStatus.Dismissed);
        _mockReportRepo.Verify(r => r.UpdateAsync(report, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetReportsAsync_ShouldReturnPaginatedList_WhenCalled()
    {
        var reports = new List<UserReport>
        {
            new UserReport { Id = Guid.NewGuid(), ReporterId = Guid.NewGuid(), TargetUserId = Guid.NewGuid(), Reason = ReportReason.Harassment, Status = ReportStatus.Pending }
        };
        _mockReportRepo.Setup(r => r.GetReportsAsync(1, 10, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((reports, 1));
        _mockUserRepo.Setup(u => u.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppUser { DisplayName = "Target User", Email = "target@test.com" });

        var result = await _adminService.GetReportsAsync(1, 10, null, CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items.First().TargetDisplayName.Should().Be("Target User");
    }

    [Fact]
    public async Task UnbanUserAsync_ShouldUnban_WhenUserExists()
    {
        var userId = Guid.NewGuid();
        var user = new AppUser { Id = userId, IsBanned = true, BanReason = "Spam" };
        _mockUserRepo.Setup(u => u.GetByIdForUpdateAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        await _adminService.UnbanUserAsync(userId, CancellationToken.None);

        user.IsBanned.Should().BeFalse();
        user.BannedUntil.Should().BeNull();
        user.BanReason.Should().BeNull();
        _mockUserRepo.Verify(u => u.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }
}
