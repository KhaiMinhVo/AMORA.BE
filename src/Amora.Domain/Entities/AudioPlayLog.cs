namespace Amora.Domain.Entities;

public sealed class AudioPlayLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? PostId { get; set; }
    public Guid? CommentId { get; set; }
    public DateTimeOffset PlayedAt { get; set; } = DateTimeOffset.UtcNow;
}
