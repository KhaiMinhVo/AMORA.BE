using Amora.Domain.Enums;

namespace Amora.Domain.Entities;

public sealed class MatchConnection
{
    public Guid Id { get; set; }

    public Guid PostId { get; set; }

    public Guid UserAId { get; set; }

    public Guid UserBId { get; set; }

    public MatchStatus Status { get; set; } = MatchStatus.Active;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}