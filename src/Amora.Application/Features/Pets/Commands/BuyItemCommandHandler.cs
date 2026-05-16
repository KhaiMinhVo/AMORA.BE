using Amora.Application.Pets;
using MediatR;

namespace Amora.Application.Features.Pets.Commands;

public sealed class BuyItemCommandHandler : IRequestHandler<BuyItemCommand>
{
    private readonly PetShopService _shopService;

    public BuyItemCommandHandler(PetShopService shopService) => _shopService = shopService;

    public Task Handle(BuyItemCommand request, CancellationToken cancellationToken)
        => _shopService.BuyAsync(request.UserId, request.Request, cancellationToken);
}
