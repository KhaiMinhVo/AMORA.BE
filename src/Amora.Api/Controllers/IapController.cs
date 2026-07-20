using Amora.Application.Abstractions;
using Amora.Application.Common;
using Amora.Application.Dtos.Iap;
using Amora.Application.Iap;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Amora.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/iap")]
public sealed class IapController : ControllerBase
{
    private readonly IapGemService _iapService;
    private readonly GooglePlayPurchaseService _googlePlayService;
    private readonly ICurrentUserService _currentUser;
    private readonly Microsoft.Extensions.Options.IOptions<IapOptions> _iapOptions;

    public IapController(
        IapGemService iapService,
        GooglePlayPurchaseService googlePlayService,
        ICurrentUserService currentUser,
        Microsoft.Extensions.Options.IOptions<IapOptions> iapOptions)
    {
        _iapService = iapService;
        _googlePlayService = googlePlayService;
        _currentUser = currentUser;
        _iapOptions = iapOptions;
    }

    /// <summary>
    /// Xac thuc receipt Apple/Google va cong Amora Gem.
    /// Idempotent theo transactionId de tranh cong trung.
    /// </summary>
    [HttpPost("verify")]
    [ProducesResponseType(typeof(ApiResponse<VerifyIapPurchaseResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<VerifyIapPurchaseResponse>>> Verify(
        [FromBody] VerifyIapPurchaseRequest request,
        CancellationToken cancellationToken)
    {
        var gemsGranted = _iapOptions.Value.Products.GetValueOrDefault(request.ProductId, 0);

        int balance;
        if (string.Equals(request.Platform, _iapOptions.Value.GooglePlatform, StringComparison.OrdinalIgnoreCase))
        {
            balance = await _googlePlayService.ProcessPurchaseAsync(
                _currentUser.UserId,
                request.ProductId,
                request.ReceiptOrToken,
                cancellationToken);
        }
        else
        {
            balance = await _iapService.VerifyAndCreditAsync(
                _currentUser.UserId,
                new IapVerificationRequest
                {
                    Platform = request.Platform,
                    ProductId = request.ProductId,
                    TransactionId = request.TransactionId,
                    ReceiptOrToken = request.ReceiptOrToken
                },
                cancellationToken);
        }

        return Ok(ApiResponse<VerifyIapPurchaseResponse>.Ok(new VerifyIapPurchaseResponse
        {
            AmoraGemsBalance = balance,
            GemsGranted = gemsGranted
        }, "Purchase verified."));
    }
}
