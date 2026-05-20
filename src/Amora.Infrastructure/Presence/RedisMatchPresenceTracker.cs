using Amora.Application.Abstractions;
using StackExchange.Redis;

namespace Amora.Infrastructure.Presence;

public sealed class RedisMatchPresenceTracker : IMatchPresenceTracker
{
    private static readonly TimeSpan OnlineWindow = TimeSpan.FromSeconds(90);
    private static readonly TimeSpan KeyTtl = TimeSpan.FromMinutes(5);
    private const string KeyPrefix = "presence:match:";

    private readonly IDatabase _db;

    public RedisMatchPresenceTracker(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public bool RecordHeartbeat(Guid matchId, Guid userId, Guid userAId, Guid userBId)
    {
        var key = GetKey(matchId);
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var cutoff = now - (long)OnlineWindow.TotalSeconds;

        _db.SortedSetAdd(key, ToMember(userId), now);
        _db.SortedSetRemoveRangeByScore(key, double.NegativeInfinity, cutoff);
        _db.KeyExpire(key, KeyTtl);

        var aScore = _db.SortedSetScore(key, ToMember(userAId));
        var bScore = _db.SortedSetScore(key, ToMember(userBId));

        return aScore.HasValue && aScore.Value >= cutoff
            && bScore.HasValue && bScore.Value >= cutoff;
    }

    public void RemoveConnection(Guid matchId, Guid userId)
    {
        _db.SortedSetRemove(GetKey(matchId), ToMember(userId));
    }

    private static string GetKey(Guid matchId) => $"{KeyPrefix}{matchId:N}";

    private static string ToMember(Guid userId) => userId.ToString("N");
}
