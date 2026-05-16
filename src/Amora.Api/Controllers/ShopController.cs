using Amora.Application.Common;
using Amora.Application.Dtos.Pets;
using Amora.Application.Features.Pets.Commands;
using Amora.Application.Features.Pets.Queries;
using Amora.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Amora.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/shop")]
public sealed class ShopController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public ShopController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet("items")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ShopItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ShopItemDto>>>> GetItems(CancellationToken cancellationToken)
    {
        var items = await _mediator.Send(new GetShopItemsQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ShopItemDto>>.Ok(items));
    }

    [HttpPost("buy")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> Buy([FromBody] BuyItemRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new BuyItemCommand(_currentUser.UserId, request), cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Purchase successful."));
    }
}
