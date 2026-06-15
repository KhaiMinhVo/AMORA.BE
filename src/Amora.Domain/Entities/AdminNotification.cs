using Amora.Domain.Enums;

namespace Amora.Domain.Entities;

public class AdminNotification
{
    public Guid Id { get; set; }
    public AdminNotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
