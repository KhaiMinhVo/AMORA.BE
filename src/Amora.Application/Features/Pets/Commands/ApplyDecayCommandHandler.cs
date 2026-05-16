using Amora.Application.Pets;
using MediatR;

namespace Amora.Application.Features.Pets.Commands;

public sealed class ApplyDecayCommandHandler : IRequestHandler<ApplyDecayCommand, int>
{
    private readonly PetCoordinator _coordinator;

    public ApplyDecayCommandHandler(PetCoordinator coordinator) => _coordinator = coordinator;

    public Task<int> Handle(ApplyDecayCommand request, CancellationToken cancellationToken)
        => _coordinator.ApplyDecayBatchAsync(cancellationToken);
}
