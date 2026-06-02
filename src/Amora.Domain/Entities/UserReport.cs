using Amora.Domain.Enums;

namespace Amora.Domain.Entities;

public sealed class UserReport
{
    public Guid Id { get; set; }

    /// <summary>Người gửi báo cáo.</summary>
    public Guid ReporterId { get; set; }

    public Guid TargetUserId { get; set; }

    /// <summary>Bài viết bị báo cáo (nếu có).</summary>
    public Guid? TargetPostId { get; set; }

    /// <summary>Bình luận bị báo cáo (nếu có).</summary>
    public Guid? TargetCommentId { get; set; }

    public ReportReason Reason { get; set; }

    /// <summary>Mô tả bổ sung (tùy chọn).</summary>
    public string? Description { get; set; }

    public ReportStatus Status { get; set; } = ReportStatus.Pending;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
