using Amora.Application.Pets;
using MediatR;

namespace Amora.Application.Features.Pets.Commands;

public sealed class PublishVoiceForVibeCommandHandler : IRequestHandler<PublishVoiceForVibeCommand>
{
    private readonly PetCoordinator _coordinator;

    public PublishVoiceForVibeCommandHandler(PetCoordinator coordinator) => _coordinator = coordinator;

    public Task Handle(PublishVoiceForVibeCommand request, CancellationToken cancellationToken)
        => _coordinator.PublishVoiceForVibeAsync(
            request.MatchId,
            request.UserId,
            request.AudioUrl,
            request.DurationSeconds,
            cancellationToken);
}
