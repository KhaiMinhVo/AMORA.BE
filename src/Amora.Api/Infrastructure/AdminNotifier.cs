using Amora.Api.Hubs;
using Amora.Application.Abstractions;
using Amora.Domain.Entities;
using Microsoft.AspNetCore.SignalR;

namespace Amora.Api.Infrastructure;

public sealed class AdminNotifier : IAdminNotifier
{
    private readonly IHubContext<AdminHub> _hubContext;

    public AdminNotifier(IHubContext<AdminHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyNewAdminAlertAsync(AdminNotification notification, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group("Admins").SendAsync(
            "ReceiveAdminNotification",
            new
            {
                Id = notification.Id,
                Type = notification.Type.ToString(),
                Title = notification.Title,
                Message = notification.Message,
                ActionUrl = notification.ActionUrl,
                CreatedAt = notification.CreatedAt,
                IsRead = notification.IsRead
            },
            cancellationToken);
    }
}
