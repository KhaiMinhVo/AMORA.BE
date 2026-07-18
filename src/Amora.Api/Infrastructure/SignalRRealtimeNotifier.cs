using Amora.Application.Abstractions;
using Amora.Domain.Entities;
using Amora.Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Amora.Api.Infrastructure;

public sealed class SignalRRealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<Hubs.ChatHub> _hubContext;
    private readonly IMatchConnectionRepository _matchRepository;

    public SignalRRealtimeNotifier(IHubContext<Hubs.ChatHub> hubContext, IMatchConnectionRepository matchRepository)
    {
        _hubContext = hubContext;
        _matchRepository = matchRepository;
    }

    public async Task NotifyMatchCreatedAsync(MatchConnection matchConnection, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            matchId = matchConnection.Id,
            postId = matchConnection.PostId,
            userAId = matchConnection.UserAId,
            userBId = matchConnection.UserBId,
            status = matchConnection.Status.ToString(),
            expiresAt = matchConnection.ExpiresAt
        };

        await _hubContext.Clients.Group(RealtimeGroupNames.User(matchConnection.UserAId.ToString()))
            .SendAsync("ReceiveMatchCreated", payload, cancellationToken);

        await _hubContext.Clients.Group(RealtimeGroupNames.User(matchConnection.UserBId.ToString()))
            .SendAsync("ReceiveMatchCreated", payload, cancellationToken);

        await _hubContext.Clients.Group(RealtimeGroupNames.Match(matchConnection.Id.ToString()))
            .SendAsync("ReceiveMatchCreated", payload, cancellationToken);
    }

    public async Task NotifyNewMessageAsync(ChatMessage message, int? unreadCount = null, DateTimeOffset? handshakeExpiresAt = null, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            messageId = message.Id,
            matchId = message.MatchId,
            senderId = message.SenderId,
            type = message.MessageType.ToString(),
            contentUrl = message.ContentUrl,
            content = message.Content,
            duration = message.Duration,
            createdAt = message.CreatedAt,
            unreadCount = unreadCount,
            expiresAt = handshakeExpiresAt
        };

        await _hubContext.Clients.Group(RealtimeGroupNames.Match(message.MatchId.ToString()))
            .SendAsync("ReceiveNewMessage", payload, cancellationToken);
    }

    public async Task NotifyMessagesReadAsync(Guid matchId, Guid userId, string lastReadMessageId, int unreadCount, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            matchId = matchId,
            userId = userId,
            lastReadMessageId = lastReadMessageId,
            unreadCount = unreadCount
        };

        await _hubContext.Clients.Group(RealtimeGroupNames.Match(matchId.ToString()))
            .SendAsync("MessagesRead", payload, cancellationToken);
    }

    public async Task NotifyMatchExpiredAsync(MatchConnection matchConnection, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            matchId = matchConnection.Id,
            reason = "Handshake24h",
            message = "Match đã hết hạn do không có tin nhắn nào trong 24 giờ.",
            expiresAt = matchConnection.ExpiresAt
        };

        await _hubContext.Clients.Group(RealtimeGroupNames.User(matchConnection.UserAId.ToString()))
            .SendAsync("ReceiveMatchExpired", payload, cancellationToken);

        await _hubContext.Clients.Group(RealtimeGroupNames.User(matchConnection.UserBId.ToString()))
            .SendAsync("ReceiveMatchExpired", payload, cancellationToken);
    }

    public async Task DisconnectUserAsync(Guid userId, string reason, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            reason = reason,
            timestamp = DateTimeOffset.UtcNow
        };

        await _hubContext.Clients.Group(RealtimeGroupNames.User(userId.ToString()))
            .SendAsync("ReceiveBanned", payload, cancellationToken);
    }

    public async Task NotifyDiamondBalanceChangedAsync(Guid userId, int newBalance, int delta, string reason, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            userId = userId,
            newBalance = newBalance,
            delta = delta,
            reason = reason,
            timestamp = DateTimeOffset.UtcNow
        };

        await _hubContext.Clients.Group(RealtimeGroupNames.User(userId.ToString()))
            .SendAsync("ReceiveDiamondBalanceChanged", payload, cancellationToken);
    }

    public async Task NotifyUserPresenceChangedAsync(Guid userId, bool isOnline, DateTimeOffset? lastActiveAt = null, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            userId = userId,
            isOnline = isOnline,
            lastActiveAt = lastActiveAt
        };

        // Notify all active matches of this user
        var activeMatches = await _matchRepository.GetActiveByUserAsync(userId, cancellationToken);
        foreach (var match in activeMatches)
        {
            await _hubContext.Clients.Group(RealtimeGroupNames.Match(match.Id.ToString()))
                .SendAsync("UserPresenceChanged", payload, cancellationToken);
        }
    }

    public async Task NotifyAdminAsync(string message, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            message = message,
            timestamp = DateTimeOffset.UtcNow
        };

        // Notify to the "Admins" group. We need to ensure admin users join this group.
        await _hubContext.Clients.Group("Admins")
            .SendAsync("ReceiveAdminNotification", payload, cancellationToken);
    }

    public async Task NotifySystemNotificationAsync(Guid userId, Notification notification, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            id = notification.Id,
            type = notification.Type.ToString(),
            title = notification.Title,
            body = notification.Body,
            isRead = notification.IsRead,
            dataJson = notification.DataJson,
            createdAt = notification.CreatedAt
        };

        await _hubContext.Clients.Group(RealtimeGroupNames.User(userId.ToString()))
            .SendAsync("ReceiveSystemNotification", payload, cancellationToken);
    }

    public async Task NotifyChatBlockStatusChangedAsync(Guid userId, Guid matchId, string blockStatus, bool canSendMessages, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            matchId = matchId,
            blockStatus = blockStatus,
            canSendMessages = canSendMessages
        };

        await _hubContext.Clients.Group(RealtimeGroupNames.User(userId.ToString()))
            .SendAsync("ChatBlockStatusChanged", payload, cancellationToken);
    }
}