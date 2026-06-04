using Amora.Domain.Enums;

namespace Amora.Domain.Entities;

public class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public string? DataJson { get; set; } // Payload for client navigation
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation property
    public AppUser? User { get; set; }
}
