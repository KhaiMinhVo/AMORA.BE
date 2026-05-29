using Amora.Api.Hubs;
using Amora.Application.Abstractions;
using Amora.Application.Pets;
using Amora.Domain.Entities;
using Microsoft.AspNetCore.SignalR;

namespace Amora.Api.Infrastructure;

public sealed class SignalRPetRealtimeNotifier : IPetRealtimeNotifier
{
    private readonly IHubContext<PetHub> _petHub;

    public SignalRPetRealtimeNotifier(IHubContext<PetHub> petHub) => _petHub = petHub;

    public async Task NotifyPetStatusUpdatedAsync(Pet pet, MatchConnection match, CancellationToken cancellationToken = default)
    {
        var dto = PetCoordinator.ToDto(pet);
        var payload = new
        {
            petId = dto.PetId,
            matchId = dto.MatchId,
            hp = dto.Hp,
            rp = dto.Rp,
            stage = dto.Stage,
            stageName = dto.StageName,
            isFrozen = dto.IsFrozen,
            unlockedFeatures = dto.UnlockedFeatures
        };

        await _petHub.Clients.Group(RealtimeGroupNames.User(match.UserAId.ToString()))
            .SendAsync("PetStatusUpdated", payload, cancellationToken);
        await _petHub.Clients.Group(RealtimeGroupNames.User(match.UserBId.ToString()))
            .SendAsync("PetStatusUpdated", payload, cancellationToken);
        await _petHub.Clients.Group(RealtimeGroupNames.Match(match.Id.ToString()))
            .SendAsync("PetStatusUpdated", payload, cancellationToken);
    }
}
