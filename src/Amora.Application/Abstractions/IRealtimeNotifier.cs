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

    Task NotifyAdminAsync(string message, CancellationToken cancellationToken = default);

    Task NotifySystemNotificationAsync(Guid userId, Notification notification, CancellationToken cancellationToken = default);

    Task NotifyImageProcessedAsync(Guid userId, string publicUrl, string imageType, CancellationToken cancellationToken = default);

    Task NotifyChatBlockStatusChangedAsync(Guid userId, Guid matchId, string blockStatus, bool canSendMessages, CancellationToken cancellationToken = default);
}