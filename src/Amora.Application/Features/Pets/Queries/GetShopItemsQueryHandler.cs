using Amora.Application.Dtos.Pets;
using Amora.Application.Pets;
using MediatR;

namespace Amora.Application.Features.Pets.Queries;

public sealed class GetShopItemsQueryHandler : IRequestHandler<GetShopItemsQuery, IReadOnlyList<ShopItemDto>>
{
    private readonly PetShopService _shopService;

    public GetShopItemsQueryHandler(PetShopService shopService) => _shopService = shopService;

    public Task<IReadOnlyList<ShopItemDto>> Handle(GetShopItemsQuery request, CancellationToken cancellationToken)
        => _shopService.GetShopItemsAsync(cancellationToken);
}
