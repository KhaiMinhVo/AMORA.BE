using Amora.Application.Pets;
using MediatR;

namespace Amora.Application.Features.Pets.Commands;

public sealed class UseItemCommandHandler : IRequestHandler<UseItemCommand>
{
    private readonly PetShopService _shopService;

    public UseItemCommandHandler(PetShopService shopService) => _shopService = shopService;

    public Task Handle(UseItemCommand request, CancellationToken cancellationToken)
        => _shopService.UseItemAsync(request.UserId, request.MatchId, request.ItemId, cancellationToken);
}
