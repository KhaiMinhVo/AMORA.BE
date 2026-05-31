using Amora.Application.Pets;
using MediatR;

namespace Amora.Application.Features.Pets.Commands;

public sealed record ClaimWaterCommand(Guid UserId, Guid MatchId) : IRequest;

public sealed class ClaimWaterCommandHandler : IRequestHandler<ClaimWaterCommand>
{
    private readonly PetShopService _petShopService;

    public ClaimWaterCommandHandler(PetShopService petShopService)
    {
        _petShopService = petShopService;
    }

    public async Task Handle(ClaimWaterCommand request, CancellationToken cancellationToken)
    {
        await _petShopService.ClaimWaterAsync(request.UserId, request.MatchId, cancellationToken);
    }
}
