using Amora.Application.Pets;
using MediatR;

namespace Amora.Application.Features.Pets.Commands;

public sealed record RenamePetCommand(Guid UserId, Guid MatchId, Guid ItemId, string NewName) : IRequest;

public sealed class RenamePetCommandHandler : IRequestHandler<RenamePetCommand>
{
    private readonly PetShopService _petShopService;

    public RenamePetCommandHandler(PetShopService petShopService)
    {
        _petShopService = petShopService;
    }

    public async Task Handle(RenamePetCommand request, CancellationToken cancellationToken)
    {
        await _petShopService.RenamePetAsync(request.UserId, request.MatchId, request.ItemId, request.NewName, cancellationToken);
    }
}
