using Amora.Domain.Entities;

namespace Amora.Application.Abstractions;

public interface IPetRealtimeNotifier
{
    Task NotifyPetStatusUpdatedAsync(Pet pet, MatchConnection match, CancellationToken cancellationToken = default);
}
