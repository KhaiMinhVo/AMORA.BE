using Amora.Application.Abstractions;
using Amora.Application.Dtos.Safety;
using Amora.Application.Exceptions;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;

namespace Amora.Application.Services;

public sealed class TrustSafetyService
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserReportRepository _reportRepository;
    private readonly IUserBlockRepository _blockRepository;
    private readonly IUserRepository _userRepository;

    public TrustSafetyService(
        ICurrentUserService currentUserService,
        IUserReportRepository reportRepository,
        IUserBlockRepository blockRepository,
        IUserRepository userRepository)
    {
        _currentUserService = currentUserService;
        _reportRepository = reportRepository;
        _blockRepository = blockRepository;
        _userRepository = userRepository;
    }

    // ── Report ──────────────────────────────────────────────────────────────

    public async Task<ReportResponseDto> ReportUserAsync(Guid targetUserId, CreateReportRequest request, CancellationToken cancellationToken = default)
    {
        var reporterId = _currentUserService.UserId;

        if (reporterId == targetUserId)
            throw new ValidationApiException("You cannot report yourself.");

        // Kiểm tra target có tồn tại không
        var target = await _userRepository.GetByIdAsync(targetUserId, cancellationToken)
            ?? throw new NotFoundApiException("User not found.");

        // Kiểm tra đã report chưa (tránh spam)
        if (await _reportRepository.ExistsAsync(reporterId, targetUserId, cancellationToken))
            throw new ConflictApiException("You have already reported this user.");

        // Parse reason enum
        if (!Enum.TryParse<ReportReason>(request.Reason, ignoreCase: true, out var reason))
            throw new ValidationApiException($"Invalid report reason. Valid values: {string.Join(", ", Enum.GetNames<ReportReason>())}");

        var report = new UserReport
        {
            Id = Guid.NewGuid(),
            ReporterId = reporterId,
            TargetUserId = targetUserId,
            Reason = reason,
            Description = request.Description,
            Status = ReportStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _reportRepository.AddAsync(report, cancellationToken);

        return new ReportResponseDto
        {
            ReportId = report.Id,
            Status = report.Status.ToString(),
            CreatedAt = report.CreatedAt
        };
    }

    // ── Block ───────────────────────────────────────────────────────────────

    public async Task<BlockResponseDto> BlockUserAsync(Guid targetUserId, CancellationToken cancellationToken = default)
    {
        var blockerId = _currentUserService.UserId;

        if (blockerId == targetUserId)
            throw new ValidationApiException("You cannot block yourself.");

        var target = await _userRepository.GetByIdAsync(targetUserId, cancellationToken)
            ?? throw new NotFoundApiException("User not found.");

        if (await _blockRepository.IsBlockedAsync(blockerId, targetUserId, cancellationToken))
            throw new ConflictApiException("You have already blocked this user.");

        var block = new UserBlock
        {
            Id = Guid.NewGuid(),
            BlockerId = blockerId,
            BlockedUserId = targetUserId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _blockRepository.AddAsync(block, cancellationToken);

        return new BlockResponseDto
        {
            BlockedUserId = targetUserId,
            Status = "Blocked"
        };
    }

    public async Task UnblockUserAsync(Guid targetUserId, CancellationToken cancellationToken = default)
    {
        var blockerId = _currentUserService.UserId;

        if (!await _blockRepository.IsBlockedAsync(blockerId, targetUserId, cancellationToken))
            throw new NotFoundApiException("This user is not in your block list.");

        await _blockRepository.RemoveAsync(blockerId, targetUserId, cancellationToken);
    }
}
