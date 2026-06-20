using Amora.Application.Pets;
using MediatR;

namespace Amora.Application.Features.Pets.Commands;

public sealed class ProcessVoiceMessagePetCommandHandler : IRequestHandler<ProcessVoiceMessagePetCommand>
{
    private readonly PetCoordinator _coordinator;

    public ProcessVoiceMessagePetCommandHandler(PetCoordinator coordinator) => _coordinator = coordinator;

    public Task Handle(ProcessVoiceMessagePetCommand request, CancellationToken cancellationToken)
        => _coordinator.ProcessVoiceMessageAsync(request.MatchId, request.SenderId, request.DurationSeconds, cancellationToken);
}
