namespace Amora.Domain.Entities;

public sealed class UserBlock
{
    public Guid Id { get; set; }

    /// <summary>Người thực hiện block.</summary>
    public Guid BlockerId { get; set; }

    /// <summary>Người bị block.</summary>
    public Guid BlockedUserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
