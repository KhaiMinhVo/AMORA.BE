using System;
using System.Threading;
using System.Threading.Tasks;
using Amora.Application.Common;
using Amora.Application.Services;
using Amora.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Amora.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/posts/{postId}/promotions")]
public sealed class PostPromotionsController : ControllerBase
{
    private readonly PostPromotionService _promotionService;

    public PostPromotionsController(PostPromotionService promotionService)
    {
        _promotionService = promotionService;
    }

    /// <summary>
    /// Boost hoac Pin bai post (24h)
    /// </summary>
    [HttpPost("boost")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> BoostPost(
        Guid postId,
        [FromBody] BoostPostRequest request,
        CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("id")?.Value;

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        await _promotionService.BoostPostAsync(userId, postId, request.BoostType, cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, $"Successfully applied {request.BoostType} to post."));
    }

    /// <summary>
    /// Mua them slot Match cho bai post
    /// </summary>
    [HttpPost("slots")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> AddMatchSlots(
        Guid postId,
        [FromBody] AddSlotsRequest request,
        CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("id")?.Value;

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        await _promotionService.AddMatchSlotsAsync(userId, postId, request.ExtraSlots, cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, $"Successfully added {request.ExtraSlots} slots to post."));
    }
}

public sealed class BoostPostRequest
{
    public PostBoostType BoostType { get; set; }
}

public sealed class AddSlotsRequest
{
    public int ExtraSlots { get; set; }
}
