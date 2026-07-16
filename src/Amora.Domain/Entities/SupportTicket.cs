using Amora.Domain.Enums;

namespace Amora.Domain.Entities;

public sealed class SupportTicket
{
    public Guid Id { get; set; }
    
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    
    public SupportTicketType Type { get; set; }
    
    /// <summary>For PaymentIssue, this holds the OrderCode or ProviderTransactionId</summary>
    public string? ReferenceId { get; set; }
    
    public string Description { get; set; } = string.Empty;
    
    public SupportTicketStatus Status { get; set; } = SupportTicketStatus.Pending;
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ResolvedAt { get; set; }
    public string? ResolutionNote { get; set; }
}
