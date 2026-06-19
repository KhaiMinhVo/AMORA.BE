using Amora.Domain.Common;
using System.ComponentModel.DataAnnotations;
using Amora.Domain.Enums;

namespace Amora.Domain.Entities;

public sealed class PaymentTransaction : BaseEntity
{
    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;

    public Guid UserId { get; set; }

    public int AmountVnd { get; set; }

    public int DiamondsReceived { get; set; }

    public long OrderCode { get; set; } // PayOS requires int64 order code

    public string Provider { get; set; } = "PayOS";

    public string? ProviderTransactionId { get; set; }

    public PaymentTransactionStatus Status { get; set; } = PaymentTransactionStatus.Pending;

    public AppUser? User { get; set; }
}
