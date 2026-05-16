using Amora.Domain.Common;

namespace Amora.Domain.Entities;

public sealed class PetStateHistory : BaseEntity
{
    public Guid PetId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = "{}";

    public Pet? Pet { get; set; }
}
