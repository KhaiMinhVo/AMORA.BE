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
    private readonly AiModerationService _aiModerationService;
    private readonly AdminModerationService _adminModerationService;
    private readonly IVoicePostRepository _voicePostRepository;
    private readonly IVoiceCommentRepository _voiceCommentRepository;

    public TrustSafetyService(
        ICurrentUserService currentUserService,
        IUserReportRepository reportRepository,
        IUserBlockRepository blockRepository,
        IUserRepository userRepository,
        AiModerationService aiModerationService,
        AdminModerationService adminModerationService,
        IVoicePostRepository voicePostRepository,
        IVoiceCommentRepository voiceCommentRepository)
    {
        _currentUserService = currentUserService;
        _reportRepository = reportRepository;
        _blockRepository = blockRepository;
        _userRepository = userRepository;
        _aiModerationService = aiModerationService;
        _adminModerationService = adminModerationService;
        _voicePostRepository = voicePostRepository;
        _voiceCommentRepository = voiceCommentRepository;
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
            TargetPostId = request.TargetPostId,
            TargetCommentId = request.TargetCommentId,
            Reason = reason,
            Description = request.Description,
            Status = ReportStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _reportRepository.AddAsync(report, cancellationToken);

        // -- AI Auto Evaluation --
        string reportedContent = "";
        if (request.TargetPostId.HasValue)
        {
            var post = await _voicePostRepository.GetByIdAsync(request.TargetPostId.Value, cancellationToken);
            if (post != null && !string.IsNullOrWhiteSpace(post.AudioUrl))
            {
                var text = await _aiModerationService.TranscribeAudioAsync(post.AudioUrl, cancellationToken);
                reportedContent = text ?? "";
            }
        }
        else if (request.TargetCommentId.HasValue)
        {
            var comment = await _voiceCommentRepository.GetByIdAsync(request.TargetCommentId.Value, cancellationToken);
            if (comment != null && !string.IsNullOrWhiteSpace(comment.AudioUrl))
            {
                var text = await _aiModerationService.TranscribeAudioAsync(comment.AudioUrl, cancellationToken);
                reportedContent = text ?? "";
            }
        }
        
        var aiVerdict = await _aiModerationService.EvaluateReportAsync(report, reportedContent, cancellationToken);
        if (aiVerdict == "BAN")
        {
            await _adminModerationService.ResolveReportAsync(report.Id, new Dtos.Admin.ResolveReportRequest
            {
                Action = "Ban",
                ResolutionNote = "[AI AUTOMATED] Banned due to severe violation.",
                BanDurationDays = 3 // Standard auto-ban duration
            }, cancellationToken);
        }
        else if (aiVerdict == "IGNORE")
        {
            await _adminModerationService.ResolveReportAsync(report.Id, new Dtos.Admin.ResolveReportRequest
            {
                Action = "Ignore",
                ResolutionNote = "[AI AUTOMATED] Ignored. Deemed safe or false report."
            }, cancellationToken);
        }

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
