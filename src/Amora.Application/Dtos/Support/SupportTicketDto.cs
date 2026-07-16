using System.ComponentModel.DataAnnotations;
using Amora.Domain.Enums;

namespace Amora.Application.Dtos.Support;

public class SupportTicketDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserDisplayName { get; set; } = string.Empty;
    public SupportTicketType Type { get; set; }
    public string? ReferenceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public SupportTicketStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public string? ResolutionNote { get; set; }
}

public class CreateTicketRequest
{
    public SupportTicketType Type { get; set; }
    
    public string? ReferenceId { get; set; }
    
    [Required(ErrorMessage = "Vui lòng nhập mô tả vấn đề.")]
    public string Description { get; set; } = string.Empty;
}

public class ResolveGeneralTicketRequest
{
    public SupportTicketStatus Status { get; set; }
    public string? ResolutionNote { get; set; }
}
