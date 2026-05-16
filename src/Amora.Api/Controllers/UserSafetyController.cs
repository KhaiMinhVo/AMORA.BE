using Amora.Application.Dtos.Safety;
using Amora.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Amora.Api.Controllers;

[ApiController]
[Route("api/users/{userId:guid}")]
public sealed class UserSafetyController : ControllerBase
{
    private readonly TrustSafetyService _trustSafetyService;

    public UserSafetyController(TrustSafetyService trustSafetyService)
    {
        _trustSafetyService = trustSafetyService;
    }

    /// <summary>Báo cáo người dùng vi phạm.</summary>
    [HttpPost("report")]
    public async Task<IActionResult> ReportUser(
        Guid userId,
        [FromBody] CreateReportRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _trustSafetyService.ReportUserAsync(userId, request, cancellationToken);
        return Ok(new { success = true, data = result });
    }

    /// <summary>Block một người dùng — không thấy nhau trên Feed nữa.</summary>
    [HttpPost("block")]
    public async Task<IActionResult> BlockUser(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _trustSafetyService.BlockUserAsync(userId, cancellationToken);
        return Ok(new { success = true, data = result });
    }

    /// <summary>Bỏ block một người dùng.</summary>
    [HttpDelete("block")]
    public async Task<IActionResult> UnblockUser(Guid userId, CancellationToken cancellationToken)
    {
        await _trustSafetyService.UnblockUserAsync(userId, cancellationToken);
        return Ok(new { success = true, message = "User unblocked." });
    }
}
