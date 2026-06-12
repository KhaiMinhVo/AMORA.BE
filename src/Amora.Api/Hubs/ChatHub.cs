using Amora.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

using Amora.Application.Abstractions;

namespace Amora.Api.Hubs;

[Authorize]
public sealed class ChatHub : Hub
{
    private readonly IUserPresenceTracker _presenceTracker;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(IUserPresenceTracker presenceTracker, ILogger<ChatHub> logger)
    {
        _presenceTracker = presenceTracker;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation(
            "ChatHub connected: UserId={UserId}, ConnectionId={ConnectionId}",
            Context.UserIdentifier,
            Context.ConnectionId);

        if (Guid.TryParse(GetUserId(), out var userId))
        {
            await _presenceTracker.UserConnectedAsync(userId, Context.ConnectionId);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation(
            exception,
            "ChatHub disconnected: UserId={UserId}, ConnectionId={ConnectionId}",
            Context.UserIdentifier,
            Context.ConnectionId);

        if (Guid.TryParse(GetUserId(), out var userId))
        {
            await _presenceTracker.UserDisconnectedAsync(userId, Context.ConnectionId);
        }
        await base.OnDisconnectedAsync(exception);
    }
    public Task JoinMyUserGroup()
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, RealtimeGroupNames.User(GetUserId()));
    }

    public Task JoinMatchGroup(string matchId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, RealtimeGroupNames.Match(matchId));
    }

    public Task LeaveMatchGroup(string matchId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, RealtimeGroupNames.Match(matchId));
    }

    private string GetUserId()
    {
        return Context.User?.FindFirst("id")?.Value
            ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? Context.ConnectionId;
    }
}