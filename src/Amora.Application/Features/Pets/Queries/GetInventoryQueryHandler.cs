using Amora.Application.Dtos.Pets;
using Amora.Application.Pets;
using MediatR;

namespace Amora.Application.Features.Pets.Queries;

public sealed class GetInventoryQueryHandler : IRequestHandler<GetInventoryQuery, IReadOnlyList<InventoryItemDto>>
{
    private readonly PetShopService _shopService;

    public GetInventoryQueryHandler(PetShopService shopService) => _shopService = shopService;

    public Task<IReadOnlyList<InventoryItemDto>> Handle(GetInventoryQuery request, CancellationToken cancellationToken)
        => _shopService.GetInventoryAsync(request.UserId, cancellationToken);
}
