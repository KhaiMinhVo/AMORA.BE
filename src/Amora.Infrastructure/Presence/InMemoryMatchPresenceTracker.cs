using System.Collections.Concurrent;
using Amora.Application.Abstractions;

namespace Amora.Infrastructure.Presence;

/// <summary>Presence in-memory (single instance). Production nhiều node → Redis.</summary>
public sealed class InMemoryMatchPresenceTracker : IMatchPresenceTracker
{
    private static readonly TimeSpan OnlineWindow = TimeSpan.FromSeconds(90);

    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, DateTimeOffset>> _matchUsers = new();

    public bool RecordHeartbeat(Guid matchId, Guid userId, Guid userAId, Guid userBId)
    {
        var now = DateTimeOffset.UtcNow;
        var users = _matchUsers.GetOrAdd(matchId, _ => new ConcurrentDictionary<Guid, DateTimeOffset>());
        users[userId] = now;

        var aOnline = IsOnline(users, userAId, now);
        var bOnline = IsOnline(users, userBId, now);
        return aOnline && bOnline;
    }

    public void RemoveConnection(Guid matchId, Guid userId)
    {
        if (_matchUsers.TryGetValue(matchId, out var users))
            users.TryRemove(userId, out _);
    }

    private static bool IsOnline(ConcurrentDictionary<Guid, DateTimeOffset> users, Guid id, DateTimeOffset now)
        => users.TryGetValue(id, out var last) && now - last <= OnlineWindow;
}
