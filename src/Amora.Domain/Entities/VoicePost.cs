using System.ComponentModel.DataAnnotations.Schema;
using Amora.Domain.Enums;

namespace Amora.Domain.Entities;

public sealed class VoicePost
{
    public Guid Id { get; set; }

    public Guid PosterId { get; set; }

    public string AudioUrl { get; set; } = string.Empty;

    public int MatchCount { get; set; }

    public VoicePostStatus Status { get; set; } = VoicePostStatus.Open;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<VoiceComment> Comments { get; set; } = new List<VoiceComment>();

    /// <summary>Dữ liệu Pet được Python Worker ghi vào sau khi xử lý xong.</summary>
    public PetVibeData? PetVibeData { get; set; }

    public int MaxMatchSlots { get; set; } = 3;

    [NotMapped]
    public bool IsBoosted { get; set; }
}