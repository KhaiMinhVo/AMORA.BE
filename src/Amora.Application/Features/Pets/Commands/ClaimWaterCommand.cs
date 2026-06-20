using Amora.Application.Pets;
using MediatR;

using Amora.Application.Dtos.Pets;

namespace Amora.Application.Features.Pets.Commands;

public sealed record ClaimWaterCommand(Guid UserId, Guid MatchId) : IRequest<WaterClaimResultDto>;

public sealed class ClaimWaterCommandHandler : IRequestHandler<ClaimWaterCommand, WaterClaimResultDto>
{
    private readonly PetShopService _petShopService;

    public ClaimWaterCommandHandler(PetShopService petShopService)
    {
        _petShopService = petShopService;
    }

    public async Task<WaterClaimResultDto> Handle(ClaimWaterCommand request, CancellationToken cancellationToken)
    {
        return await _petShopService.ClaimWaterAsync(request.UserId, request.MatchId, cancellationToken);
    }
}
