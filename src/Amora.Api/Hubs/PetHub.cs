using Amora.Application.Abstractions;
using Amora.Application.Features.Pets.Commands;
using Amora.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Amora.Api.Hubs;

[Authorize]
public sealed class PetHub : Hub
{
    private readonly IMediator _mediator;
    private readonly IMatchPresenceTracker _presence;
    private readonly IMatchConnectionRepository _matches;

    public PetHub(IMediator mediator, IMatchPresenceTracker presence, IMatchConnectionRepository matches)
    {
        _mediator = mediator;
        _presence = presence;
        _matches = matches;
    }

    public Task JoinMyUserGroup()
        => Groups.AddToGroupAsync(Context.ConnectionId, Infrastructure.RealtimeGroupNames.User(GetUserId()));

    public Task JoinMatchGroup(string matchId)
        => Groups.AddToGroupAsync(Context.ConnectionId, Infrastructure.RealtimeGroupNames.Match(matchId));

    /// <summary>Heartbeat ~30s — khi cả hai user online, cộng +5 RP/ngày.</summary>
    public async Task Heartbeat(string matchId)
    {
        if (!Guid.TryParse(matchId, out var matchGuid) || !Guid.TryParse(GetUserId(), out var userId))
            return;

        var match = await _matches.GetByIdAsync(matchGuid);
        if (match is null) return;

        if (!await _matches.IsParticipantAsync(matchGuid, userId))
            return;

        var coPresent = _presence.RecordHeartbeat(matchGuid, userId, match.UserAId, match.UserBId);
        if (coPresent)
            await _mediator.Send(new ProcessCoPresenceCommand(matchGuid));
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Best-effort cleanup — client nên gọi LeaveMatch trước khi disconnect
        await base.OnDisconnectedAsync(exception);
    }

    public Task LeaveMatch(string matchId)
    {
        if (Guid.TryParse(matchId, out var matchGuid) && Guid.TryParse(GetUserId(), out var userId))
            _presence.RemoveConnection(matchGuid, userId);

        return Groups.RemoveFromGroupAsync(Context.ConnectionId, Infrastructure.RealtimeGroupNames.Match(matchId));
    }

    private string GetUserId()
        => Context.User?.FindFirst("id")?.Value
           ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
           ?? Context.ConnectionId;
}
