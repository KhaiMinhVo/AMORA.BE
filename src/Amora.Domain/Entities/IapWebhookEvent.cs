using Amora.Domain.Common;

namespace Amora.Domain.Entities;

/// <summary>Audit log for every incoming IAP webhook — idempotency key = Platform + EventId.</summary>
public sealed class IapWebhookEvent : BaseEntity
{
    /// <summary>Apple or Google.</summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>Unique event identifier (Apple: signedPayload hash, Google: Pub/Sub messageId).</summary>
    public string EventId { get; set; } = string.Empty;

    /// <summary>REFUND, DID_RENEW, SUBSCRIBED, etc.</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>The transactionId / purchaseToken extracted from the payload.</summary>
    public string? TransactionId { get; set; }

    /// <summary>Whether the event was actually processed or was a duplicate.</summary>
    public bool Processed { get; set; }

    /// <summary>Raw payload (truncated to 4 KB) for debugging.</summary>
    public string? RawPayload { get; set; }
}
