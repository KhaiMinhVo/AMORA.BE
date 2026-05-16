using Amora.Application.Abstractions;
using Amora.Domain.Entities;
using Microsoft.AspNetCore.SignalR;

namespace Amora.Api.Infrastructure;

public sealed class SignalRRealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<Hubs.ChatHub> _hubContext;

    public SignalRRealtimeNotifier(IHubContext<Hubs.ChatHub> hubContext)
    {
        _hubContext = hubContext;
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

    public async Task NotifyNewMessageAsync(ChatMessage message, DateTimeOffset? handshakeExpiresAt = null, CancellationToken cancellationToken = default)
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
            expiresAt = handshakeExpiresAt
        };

        await _hubContext.Clients.Group(RealtimeGroupNames.Match(message.MatchId.ToString()))
            .SendAsync("ReceiveNewMessage", payload, cancellationToken);
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
}