namespace Amora.Domain.Entities;

public class UserPushToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty; // "android" | "ios"
    public string Token { get; set; } = string.Empty;     // ExponentPushToken[...]
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastActiveAt { get; set; }

    // Navigation
    public AppUser? User { get; set; }
}
