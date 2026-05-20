using Amora.Domain.Common;

namespace Amora.Domain.Entities;

/// <summary>Ghi nhận IAP đã xử lý — chống cộng gem trùng transactionId.</summary>
public sealed class IapPurchaseRecord : BaseEntity
{
    public Guid UserId { get; set; }

    public string Platform { get; set; } = string.Empty;

    public string TransactionId { get; set; } = string.Empty;

    public string ProductId { get; set; } = string.Empty;

    public int GemsGranted { get; set; }

    public DateTimeOffset? RefundedAt { get; set; }

    public string? RefundReason { get; set; }

    public AppUser? User { get; set; }
}
