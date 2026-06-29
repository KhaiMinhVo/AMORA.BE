using System;
using System.Threading;
using System.Threading.Tasks;
using Amora.Application.Common;
using Amora.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Amora.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users/promotions")]
public sealed class UserPromotionsController : ControllerBase
{
    private readonly UserPromotionService _promotionService;

    public UserPromotionsController(UserPromotionService promotionService)
    {
        _promotionService = promotionService;
    }

    /// <summary>
    /// Mua them slot Match cho tai khoan
    /// </summary>
    [HttpPost("slots")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> AddMatchSlots(
        [FromBody] AddUserSlotsRequest request,
        CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("id")?.Value;

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        await _promotionService.AddMatchSlotsAsync(userId, request.ExtraSlots, cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, $"Successfully added {request.ExtraSlots} match slots."));
    }
}

public sealed class AddUserSlotsRequest
{
    public int ExtraSlots { get; set; }
}
