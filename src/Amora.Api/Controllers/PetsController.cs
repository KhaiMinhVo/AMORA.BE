using Amora.Application.Common;
using Amora.Application.Dtos.Pets;
using Amora.Application.Features.Pets.Commands;
using Amora.Application.Features.Pets.Queries;
using Amora.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Amora.Api.Controllers;

/// <summary>Thú cưng ảo theo match — HP, Mood, RP, tiến hóa.</summary>
[ApiController]
[Authorize]
[Route("api/pets")]
public sealed class PetsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public PetsController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>Trạng thái Pet của match.</summary>
    [HttpGet("{matchId:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<PetStatusDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PetStatusDto>>> GetStatus(Guid matchId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPetStatusQuery(matchId, _currentUser.UserId), cancellationToken);
        return Ok(ApiResponse<PetStatusDto>.Ok(result));
    }

    /// <summary>Dùng item từ inventory lên Pet của match.</summary>
    [HttpPost("{matchId:guid}/use-item")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> UseItem(
        Guid matchId,
        [FromBody] UseItemRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new UseItemCommand(_currentUser.UserId, matchId, request.ItemId), cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Item used successfully."));
    }
}
