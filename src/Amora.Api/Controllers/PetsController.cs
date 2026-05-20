using Amora.Application.Common;
using Amora.Application.Dtos.Pets;
using Amora.Application.Features.Pets.Commands;
using Amora.Application.Features.Pets.Queries;
using Amora.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Amora.Api.Controllers;

/// <summary>
/// Thu cung ao theo match (HP, Mood, RP, tien hoa).
/// Cung cap API xem trang thai va dung item.
/// </summary>
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

    /// <summary>
    /// Lay trang thai Pet cua match.
    /// Tra ve cac chi so hien tai cua Pet.
    /// </summary>
    [HttpGet("{matchId:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<PetStatusDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PetStatusDto>>> GetStatus(Guid matchId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPetStatusQuery(matchId, _currentUser.UserId), cancellationToken);
        return Ok(ApiResponse<PetStatusDto>.Ok(result));
    }

    /// <summary>
    /// Su dung item tu inventory len Pet cua match.
    /// Cap nhat trang thai Pet sau khi ap dung.
    /// </summary>
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
