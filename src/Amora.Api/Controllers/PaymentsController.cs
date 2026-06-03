using Amora.Application.Abstractions;
using Amora.Application.Common;
using Amora.Application.Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Amora.Api.Controllers;

[ApiController]
[Route("api/payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly PaymentService _paymentService;
    private readonly Amora.Application.Payment.PayOs.PayOsService _payOsService;
    private readonly ICurrentUserService _currentUser;

    public PaymentsController(
        PaymentService paymentService, 
        Amora.Application.Payment.PayOs.PayOsService payOsService, 
        ICurrentUserService currentUser)
    {
        _paymentService = paymentService;
        _payOsService = payOsService;
        _currentUser = currentUser;
    }

    public sealed class CreateVnPayUrlRequest
    {
        public int Diamonds { get; set; }
    }

    [Authorize]
    [HttpPost("vnpay/create-url")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<string>>> CreateVnPayUrl(
        [FromBody] CreateVnPayUrlRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Diamonds <= 0)
        {
            return BadRequest(ApiResponse<string>.Fail("Số kim cương phải lớn hơn 0.", "INVALID_DIAMONDS"));
        }

        var url = await _paymentService.CreateVnPayUrlAsync(_currentUser.UserId, request.Diamonds, cancellationToken);
        return Ok(ApiResponse<string>.Ok(url, "Success"));
    }

    [AllowAnonymous]
    [HttpGet("vnpay/callback")]
    public async Task<IActionResult> VnPayCallback(CancellationToken cancellationToken)
    {
        var queryDictionary = HttpContext.Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
        
        var result = await _paymentService.ProcessVnPayCallbackAsync(queryDictionary, cancellationToken);
        
        if (result.Success)
        {
            return Ok("Giao dịch thành công. Vui lòng quay lại ứng dụng.");
        }
        else
        {
            return BadRequest($"Giao dịch thất bại hoặc có lỗi xảy ra. {result.Message}");
        }
    }

    [AllowAnonymous]
    [HttpGet("vnpay/ipn")]
    public async Task<IActionResult> VnPayIpn(CancellationToken cancellationToken)
    {
        var queryDictionary = HttpContext.Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
        
        var result = await _paymentService.ProcessVnPayCallbackAsync(queryDictionary, cancellationToken);
        
        return Ok(new { RspCode = result.RspCode, Message = result.Message });
    }

    public sealed class CreatePayOsUrlRequest
    {
        public int Diamonds { get; set; }
    }

    [Authorize]
    [HttpPost("payos/create-url")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<string>>> CreatePayOsUrl(
        [FromBody] CreatePayOsUrlRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Diamonds <= 0)
        {
            return BadRequest(ApiResponse<string>.Fail("Số kim cương phải lớn hơn 0.", "INVALID_DIAMONDS"));
        }

        var url = await _payOsService.CreatePayOsUrlAsync(_currentUser.UserId, request.Diamonds, cancellationToken);
        return Ok(ApiResponse<string>.Ok(url, "Success"));
    }



    [AllowAnonymous]
    [HttpPost("payos/ipn")]
    public async Task<IActionResult> PayOsIpn([FromBody] global::PayOS.Models.Webhooks.Webhook body, CancellationToken cancellationToken)
    {
        var success = await _payOsService.VerifyPaymentWebhookAsync(body, cancellationToken);
        if (success)
        {
            return Ok(new { success = true });
        }
        return BadRequest(new { success = false });
    }
}
