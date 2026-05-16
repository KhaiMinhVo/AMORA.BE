using Amora.Application.Pets;
using Amora.Domain.Entities;
using MediatR;

namespace Amora.Application.Features.Pets.Commands;

public sealed class CreatePetCommandHandler : IRequestHandler<CreatePetCommand, Pet>
{
    private readonly PetCoordinator _coordinator;

    public CreatePetCommandHandler(PetCoordinator coordinator) => _coordinator = coordinator;

    public Task<Pet> Handle(CreatePetCommand request, CancellationToken cancellationToken)
        => _coordinator.CreateForMatchAsync(request.MatchId, cancellationToken);
}
