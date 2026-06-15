using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Amora.Api.Hubs;

[Authorize(Roles = "Admin")]
public sealed class AdminHub : Hub
{
    private readonly ILogger<AdminHub> _logger;

    public AdminHub(ILogger<AdminHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("Admin connected: UserId={UserId}, ConnectionId={ConnectionId}", userId, Context.ConnectionId);

        // Add all connected admins to a shared group to broadcast to them
        await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation(exception, "Admin disconnected: UserId={UserId}, ConnectionId={ConnectionId}", userId, Context.ConnectionId);

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Admins");

        await base.OnDisconnectedAsync(exception);
    }
}
