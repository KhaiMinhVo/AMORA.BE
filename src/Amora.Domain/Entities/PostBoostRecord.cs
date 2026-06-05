using System;
using Amora.Domain.Enums;

namespace Amora.Domain.Entities;

public sealed class PostBoostRecord
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }
    public Guid UserId { get; set; }
    
    public PostBoostType BoostType { get; set; } = PostBoostType.None;
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
}
