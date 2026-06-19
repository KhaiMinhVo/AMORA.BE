using Amora.Domain.Common;

namespace Amora.Domain.Entities;

public sealed class PetActivity : BaseEntity
{
    public Guid MatchId { get; set; }
    
    public Guid PetId { get; set; }
    
    public Guid UserId { get; set; }
    
    public string ActivityType { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public MatchConnection? Match { get; set; }
    
    public Pet? Pet { get; set; }
    
    public AppUser? User { get; set; }
}
