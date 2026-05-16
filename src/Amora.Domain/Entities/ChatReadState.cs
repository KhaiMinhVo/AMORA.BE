using Amora.Domain.Common;

namespace Amora.Domain.Entities;

/// <summary>Điểm đọc tin nhắn cuối của user trong một match.</summary>
public sealed class ChatReadState : BaseEntity
{
    public Guid UserId { get; set; }

    public Guid MatchId { get; set; }

    public DateTimeOffset LastReadAt { get; set; }

    public AppUser? User { get; set; }

    public MatchConnection? Match { get; set; }
}
