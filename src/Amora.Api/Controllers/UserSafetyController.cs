using Amora.Application.Dtos.Safety;
using Amora.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Amora.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UserSafetyController : ControllerBase
{
    private readonly TrustSafetyService _trustSafetyService;

    public UserSafetyController(TrustSafetyService trustSafetyService)
    {
        _trustSafetyService = trustSafetyService;
    }

    /// <summary>
    /// Bao cao nguoi dung vi pham.
    /// Luu thong tin ly do va mo ta.
    /// </summary>
    [HttpPost("{userId:guid}/report")]
    public async Task<IActionResult> ReportUser(
        Guid userId,
        [FromBody] CreateReportRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _trustSafetyService.ReportUserAsync(userId, request, cancellationToken);
        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// Block mot nguoi dung de an nhau tren feed.
    /// Tao ban ghi chan tuong tac.
    /// </summary>
    [HttpPost("{userId:guid}/block")]
    public async Task<IActionResult> BlockUser(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _trustSafetyService.BlockUserAsync(userId, cancellationToken);
        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// Bo block mot nguoi dung.
    /// Khoi phuc kha nang tuong tuong tac.
    /// </summary>
    [HttpDelete("{userId:guid}/block")]
    public async Task<IActionResult> UnblockUser(Guid userId, CancellationToken cancellationToken)
    {
        await _trustSafetyService.UnblockUserAsync(userId, cancellationToken);
        return Ok(new { success = true, message = "User unblocked." });
    }

    /// <summary>
    /// Lay danh sach nguoi dung dang bi ban block.
    /// </summary>
    [HttpGet("me/blocks")]
    public async Task<IActionResult> GetBlockedUsers(CancellationToken cancellationToken)
    {
        var result = await _trustSafetyService.GetBlockedUsersAsync(cancellationToken);
        return Ok(new { success = true, data = result });
    }
}
