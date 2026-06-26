using Amora.Domain.Enums;

namespace Amora.Domain.Entities;

public sealed class PostReaction
{
    public Guid Id { get; set; }
    
    public Guid PostId { get; set; }
    
    public Guid UserId { get; set; }
    
    public ReactionType Type { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    public VoicePost Post { get; set; } = null!;
    
    public AppUser User { get; set; } = null!;
}
