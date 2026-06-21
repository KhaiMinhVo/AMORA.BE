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
[Route("api/subscriptions")]
public sealed class SubscriptionsController : ControllerBase
{
    private readonly SubscriptionService _subscriptionService;

    public SubscriptionsController(SubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    /// <summary>
    /// Mua goi Premium / Gold bang Kim Cuong
    /// </summary>
    [HttpPost("buy")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> BuySubscription(
        [FromBody] BuySubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("id")?.Value;

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        await _subscriptionService.PurchaseSubscriptionAsync(
            userId, 
            request.Type, 
            request.DurationDays, 
            request.PriceDiamonds, 
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, $"Successfully purchased {request.Type} for {request.DurationDays} days."));
    }

    /// <summary>
    /// Hủy gói Premium / Gold hiện tại (quay về Free)
    /// </summary>
    [HttpPost("cancel")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> CancelSubscription(CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("id")?.Value;

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        await _subscriptionService.CancelSubscriptionAsync(userId, cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, "Đã hủy gói đăng ký thành công. Bạn đã trở về gói Free."));
    }
}

public sealed class BuySubscriptionRequest
{
    public SubscriptionType Type { get; set; }
    public int DurationDays { get; set; }
    public int PriceDiamonds { get; set; }
}
