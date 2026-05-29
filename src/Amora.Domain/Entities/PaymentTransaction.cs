using Amora.Domain.Common;
using Amora.Domain.Enums;

namespace Amora.Domain.Entities;

public sealed class PaymentTransaction : BaseEntity
{
    public Guid UserId { get; set; }

    public int AmountVnd { get; set; }

    public int DiamondsReceived { get; set; }

    public string Provider { get; set; } = "VNPay";

    public string? ProviderTransactionId { get; set; }

    public PaymentTransactionStatus Status { get; set; } = PaymentTransactionStatus.Pending;

    public AppUser? User { get; set; }
}
