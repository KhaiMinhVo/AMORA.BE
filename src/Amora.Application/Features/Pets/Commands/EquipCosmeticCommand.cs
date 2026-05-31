using Amora.Application.Pets;
using MediatR;

namespace Amora.Application.Features.Pets.Commands;

public sealed record EquipCosmeticCommand(Guid UserId, Guid MatchId, Guid ItemId) : IRequest;

public sealed class EquipCosmeticCommandHandler : IRequestHandler<EquipCosmeticCommand>
{
    private readonly PetShopService _petShopService;

    public EquipCosmeticCommandHandler(PetShopService petShopService)
    {
        _petShopService = petShopService;
    }

    public async Task Handle(EquipCosmeticCommand request, CancellationToken cancellationToken)
    {
        await _petShopService.EquipCosmeticAsync(request.UserId, request.MatchId, request.ItemId, cancellationToken);
    }
}
