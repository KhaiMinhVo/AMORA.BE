using System;
using Amora.Domain.Enums;

namespace Amora.Domain.Entities;

public class UserBan
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    
    public string BanReason { get; set; } = string.Empty;
    public DateTimeOffset? BannedUntil { get; set; }
    
    // Appeal information
    public string? AppealReason { get; set; }
    public AppealStatus? AppealStatus { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    
    // True if this ban is currently in effect
    public bool IsActive { get; set; }
}
