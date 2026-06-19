using Amora.Domain.Entities;

namespace Amora.Application.Abstractions;

public interface IRealtimeNotifier
{
    Task NotifyMatchCreatedAsync(MatchConnection matchConnection, CancellationToken cancellationToken = default);

    Task NotifyNewMessageAsync(ChatMessage message, int? unreadCount = null, DateTimeOffset? handshakeExpiresAt = null, CancellationToken cancellationToken = default);

    Task NotifyMessagesReadAsync(Guid matchId, Guid userId, string lastReadMessageId, int unreadCount, CancellationToken cancellationToken = default);

    Task NotifyMatchExpiredAsync(MatchConnection matchConnection, CancellationToken cancellationToken = default);

    Task DisconnectUserAsync(Guid userId, string reason, CancellationToken cancellationToken = default);

    Task NotifyUserPresenceChangedAsync(Guid userId, bool isOnline, DateTimeOffset? lastActiveAt = null, CancellationToken cancellationToken = default);

    Task NotifyDiamondBalanceChangedAsync(Guid userId, int newBalance, int delta, string reason, CancellationToken cancellationToken = default);
}