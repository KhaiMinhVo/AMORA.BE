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
    private readonly AdminNotificationService _adminNotificationService;
    private readonly TrustScoreService _trustScoreService;
    private readonly IMatchConnectionRepository _matchConnectionRepository;
    private readonly IRealtimeNotifier _realtimeNotifier;

    public TrustSafetyService(
        ICurrentUserService currentUserService,
        IUserReportRepository reportRepository,
        IUserBlockRepository blockRepository,
        IUserRepository userRepository,
        AiModerationService aiModerationService,
        AdminModerationService adminModerationService,
        IVoicePostRepository voicePostRepository,
        IVoiceCommentRepository voiceCommentRepository,
        AdminNotificationService adminNotificationService,
        TrustScoreService trustScoreService,
        IMatchConnectionRepository matchConnectionRepository,
        IRealtimeNotifier realtimeNotifier)
    {
        _currentUserService = currentUserService;
        _reportRepository = reportRepository;
        _blockRepository = blockRepository;
        _userRepository = userRepository;
        _aiModerationService = aiModerationService;
        _adminModerationService = adminModerationService;
        _voicePostRepository = voicePostRepository;
        _voiceCommentRepository = voiceCommentRepository;
        _adminNotificationService = adminNotificationService;
        _trustScoreService = trustScoreService;
        _matchConnectionRepository = matchConnectionRepository;
        _realtimeNotifier = realtimeNotifier;
    }

    // ── Report ──────────────────────────────────────────────────────────────

    public async Task<ReportResponseDto> ReportUserAsync(Guid targetUserId, CreateReportRequest request, CancellationToken cancellationToken = default)
    {
        var reporterId = _currentUserService.UserId;

        if (reporterId == targetUserId)
            throw new ValidationApiException("Bạn không thể báo cáo chính mình.");

        // Kiểm tra target có tồn tại không
        var target = await _userRepository.GetByIdAsync(targetUserId, cancellationToken)
            ?? throw new NotFoundApiException("Không tìm thấy người dùng.");

        // Kiểm tra đã report chưa (tránh spam)
        if (await _reportRepository.ExistsAsync(reporterId, targetUserId, cancellationToken))
            throw new ConflictApiException("Bạn đã báo cáo người dùng này rồi.");

        // Parse reason enum
        if (!Enum.TryParse<ReportReason>(request.Reason, ignoreCase: true, out var reason))
            throw new ValidationApiException($"Lý do báo cáo không hợp lệ. Các giá trị cho phép: {string.Join(", ", Enum.GetNames<ReportReason>())}");

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
        
        string targetType = request.TargetPostId.HasValue ? "VoicePost" : (request.TargetCommentId.HasValue ? "VoiceComment" : "User");
        var reporter = await _userRepository.GetByIdAsync(reporterId, cancellationToken);
        await _adminNotificationService.NotifyNewReportAsync(
            reporterId, 
            reporter?.DisplayName ?? reporterId.ToString()[..8],
            targetUserId, 
            target.DisplayName,
            targetType, 
            request.Reason, 
            cancellationToken);

        await _trustScoreService.DeductReportPenaltyAsync(targetUserId, cancellationToken);

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
            throw new ValidationApiException("Bạn không thể chặn chính mình.");

        var target = await _userRepository.GetByIdAsync(targetUserId, cancellationToken)
            ?? throw new NotFoundApiException("Không tìm thấy người dùng.");

        if (await _blockRepository.IsBlockedAsync(blockerId, targetUserId, cancellationToken))
            throw new ConflictApiException("Bạn đã chặn người dùng này rồi.");

        var block = new UserBlock
        {
            Id = Guid.NewGuid(),
            BlockerId = blockerId,
            BlockedUserId = targetUserId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _blockRepository.AddAsync(block, cancellationToken);

        var match = await _matchConnectionRepository.GetMatchBetweenUsersAsync(blockerId, targetUserId, cancellationToken);
        if (match != null)
        {
            await _realtimeNotifier.NotifyChatBlockStatusChangedAsync(blockerId, match.Id, "BlockedByMe", false, cancellationToken);
            await _realtimeNotifier.NotifyChatBlockStatusChangedAsync(targetUserId, match.Id, "BlockedMe", false, cancellationToken);
        }

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
            throw new NotFoundApiException("Người dùng này không nằm trong danh sách chặn của bạn.");

        await _blockRepository.RemoveAsync(blockerId, targetUserId, cancellationToken);

        var match = await _matchConnectionRepository.GetMatchBetweenUsersAsync(blockerId, targetUserId, cancellationToken);
        if (match != null)
        {
            // Kiểm tra xem đầu kia có đang block lại mình không (mutual block)
            bool isBlockedByThem = await _blockRepository.IsBlockedAsync(targetUserId, blockerId, cancellationToken);
            
            if (isBlockedByThem)
            {
                await _realtimeNotifier.NotifyChatBlockStatusChangedAsync(blockerId, match.Id, "BlockedMe", false, cancellationToken);
                await _realtimeNotifier.NotifyChatBlockStatusChangedAsync(targetUserId, match.Id, "BlockedByMe", false, cancellationToken);
            }
            else
            {
                await _realtimeNotifier.NotifyChatBlockStatusChangedAsync(blockerId, match.Id, "None", true, cancellationToken);
                await _realtimeNotifier.NotifyChatBlockStatusChangedAsync(targetUserId, match.Id, "None", true, cancellationToken);
            }
        }
    }

    public async Task<IEnumerable<BlockedUserDto>> GetBlockedUsersAsync(CancellationToken cancellationToken = default)
    {
        var blockerId = _currentUserService.UserId;
        var blocks = await _blockRepository.GetBlockedUsersAsync(blockerId, cancellationToken);

        var dtos = new List<BlockedUserDto>();
        foreach (var b in blocks)
        {
            var u = await _userRepository.GetByIdAsync(b.BlockedUserId, cancellationToken);
            if (u != null)
            {
                dtos.Add(new BlockedUserDto
                {
                    UserId = u.Id,
                    DisplayName = u.DisplayName,
                    AvatarUrl = u.AvatarUrl,
                    BlockedAt = b.CreatedAt
                });
            }
        }
        return dtos;
    }
}
