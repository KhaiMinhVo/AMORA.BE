using Amora.Domain.Enums;

namespace Amora.Domain.Entities;

public sealed class VoiceComment
{
    public Guid Id { get; set; }

    public Guid PostId { get; set; }

    public VoicePost? Post { get; set; }

    public Guid CommenterId { get; set; }

    public string AudioUrl { get; set; } = string.Empty;

    public int Duration { get; set; }

    public VoiceCommentStatus Status { get; set; } = VoiceCommentStatus.Pending;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}