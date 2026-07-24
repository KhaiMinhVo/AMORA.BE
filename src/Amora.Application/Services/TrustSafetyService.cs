using Amora.Application.Abstractions;
using Amora.Application.Dtos.Safety;
using Amora.Application.Exceptions;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Amora.Application.Services;

public sealed class TrustSafetyService
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserReportRepository _reportRepository;
    private readonly IUserBlockRepository _blockRepository;
    private readonly IUserRepository _userRepository;
    private readonly AiModerationService _aiModerationService;
    private readonly IVoicePostRepository _voicePostRepository;
    private readonly IVoiceCommentRepository _voiceCommentRepository;
    private readonly AdminNotificationService _adminNotificationService;
    private readonly IMatchConnectionRepository _matchConnectionRepository;
    private readonly IRealtimeNotifier _realtimeNotifier;
    private readonly Microsoft.Extensions.Logging.ILogger<TrustSafetyService> _logger;

    public TrustSafetyService(
        ICurrentUserService currentUserService,
        IUserReportRepository reportRepository,
        IUserBlockRepository blockRepository,
        IUserRepository userRepository,
        AiModerationService aiModerationService,
        IVoicePostRepository voicePostRepository,
        IVoiceCommentRepository voiceCommentRepository,
        AdminNotificationService adminNotificationService,
        IMatchConnectionRepository matchConnectionRepository,
        IRealtimeNotifier realtimeNotifier,
        Microsoft.Extensions.Logging.ILogger<TrustSafetyService> logger)
    {
        _currentUserService = currentUserService;
        _reportRepository = reportRepository;
        _blockRepository = blockRepository;
        _userRepository = userRepository;
        _aiModerationService = aiModerationService;
        _voicePostRepository = voicePostRepository;
        _voiceCommentRepository = voiceCommentRepository;
        _adminNotificationService = adminNotificationService;
        _matchConnectionRepository = matchConnectionRepository;
        _realtimeNotifier = realtimeNotifier;
        _logger = logger;
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
        if (await _reportRepository.ExistsRecentAsync(
                reporterId,
                targetUserId,
                request.TargetPostId,
                request.TargetCommentId,
                DateTimeOffset.UtcNow.AddHours(-24),
                cancellationToken))
            throw new ConflictApiException("Bạn đã báo cáo người dùng này rồi.");

        // Parse reason enum
        if (!Enum.TryParse<ReportReason>(request.Reason, ignoreCase: true, out var reason))
            throw new ValidationApiException($"Lý do báo cáo không hợp lệ. Các giá trị cho phép: {string.Join(", ", Enum.GetNames<ReportReason>())}");

        if (request.TargetPostId.HasValue && request.TargetCommentId.HasValue)
            throw new ValidationApiException("Mỗi báo cáo chỉ được đính kèm một bài viết hoặc một bình luận.");

        VoicePost? targetPost = null;
        VoiceComment? targetComment = null;

        if (request.TargetPostId.HasValue)
        {
            targetPost = await _voicePostRepository.GetByIdAsync(request.TargetPostId.Value, cancellationToken)
                ?? throw new NotFoundApiException("Bài viết bị báo cáo không tồn tại.");

            if (targetPost.PosterId != targetUserId)
                throw new ValidationApiException("Bài viết không thuộc về người dùng bị báo cáo.");
        }
        else if (request.TargetCommentId.HasValue)
        {
            targetComment = await _voiceCommentRepository.GetByIdAsync(request.TargetCommentId.Value, cancellationToken)
                ?? throw new NotFoundApiException("Bình luận bị báo cáo không tồn tại.");

            if (targetComment.CommenterId != targetUserId)
                throw new ValidationApiException("Bình luận không thuộc về người dùng bị báo cáo.");
        }

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

        // -- AI Auto Evaluation --
        try
        {
            string reportedContent = "";
            if (targetPost is not null && !string.IsNullOrWhiteSpace(targetPost.AudioUrl))
            {
                var text = await _aiModerationService.TranscribeAudioAsync(targetPost.AudioUrl, cancellationToken);
                reportedContent = text ?? "";
            }
            else if (targetComment is not null && !string.IsNullOrWhiteSpace(targetComment.AudioUrl))
            {
                var text = await _aiModerationService.TranscribeAudioAsync(targetComment.AudioUrl, cancellationToken);
                reportedContent = text ?? "";
            }

            if (!string.IsNullOrWhiteSpace(reportedContent))
            {
                var evaluation = await _aiModerationService.EvaluateReportAsync(reportedContent, cancellationToken);
                report.AiVerdict = evaluation.Verdict;
                report.AiScore = evaluation.Score;
                report.AiEvaluatedAt = DateTimeOffset.UtcNow;
                await _reportRepository.UpdateAiEvaluationAsync(
                    report.Id,
                    evaluation.Verdict,
                    evaluation.Score,
                    report.AiEvaluatedAt.Value,
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI advisory evaluation failed for report {ReportId}.", report.Id);
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
