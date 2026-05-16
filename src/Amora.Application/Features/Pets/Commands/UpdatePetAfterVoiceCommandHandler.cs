using Amora.Application.Pets;
using MediatR;

namespace Amora.Application.Features.Pets.Commands;

public sealed class UpdatePetAfterVoiceCommandHandler : IRequestHandler<UpdatePetAfterVoiceCommand>
{
    private readonly PetCoordinator _coordinator;

    public UpdatePetAfterVoiceCommandHandler(PetCoordinator coordinator) => _coordinator = coordinator;

    public Task Handle(UpdatePetAfterVoiceCommand request, CancellationToken cancellationToken)
        => _coordinator.ProcessVibeResultAsync(request.Result, cancellationToken);
}
