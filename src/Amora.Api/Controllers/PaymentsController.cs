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
    private readonly ICurrentUserService _currentUser;

    public PaymentsController(PaymentService paymentService, ICurrentUserService currentUser)
    {
        _paymentService = paymentService;
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

    [HttpGet("vnpay/callback")]
    public async Task<IActionResult> VnPayCallback(CancellationToken cancellationToken)
    {
        var queryDictionary = HttpContext.Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
        
        var success = await _paymentService.ProcessVnPayCallbackAsync(queryDictionary, cancellationToken);
        
        if (success)
        {
            return Ok("Giao dịch thành công. Vui lòng quay lại ứng dụng.");
        }
        else
        {
            return BadRequest("Giao dịch thất bại hoặc có lỗi xảy ra.");
        }
    }
}
