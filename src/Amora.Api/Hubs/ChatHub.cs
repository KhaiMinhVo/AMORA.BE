using Amora.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Amora.Api.Hubs;

[Authorize]
public sealed class ChatHub : Hub
{
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