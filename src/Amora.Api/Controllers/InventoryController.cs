using Amora.Application.Common;
using Amora.Application.Dtos.Pets;
using Amora.Application.Features.Pets.Queries;
using Amora.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Amora.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/inventory")]
public sealed class InventoryController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public InventoryController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Lay danh sach item trong kho cua user hien tai.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<InventoryItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<InventoryItemDto>>>> GetInventory(CancellationToken cancellationToken)
    {
        var items = await _mediator.Send(new GetInventoryQuery(_currentUser.UserId), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<InventoryItemDto>>.Ok(items));
    }
}
