using Amora.Application.Pets;
using MediatR;

namespace Amora.Application.Features.Pets.Commands;

public sealed class ProcessTextMessagePetCommandHandler : IRequestHandler<ProcessTextMessagePetCommand>
{
    private readonly PetCoordinator _coordinator;

    public ProcessTextMessagePetCommandHandler(PetCoordinator coordinator) => _coordinator = coordinator;

    public Task Handle(ProcessTextMessagePetCommand request, CancellationToken cancellationToken)
        => _coordinator.ProcessTextMessageAsync(request.MatchId, request.SenderId, cancellationToken);
}
