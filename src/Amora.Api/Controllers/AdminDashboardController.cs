using Amora.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Amora.Api.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = "Admin,Moderator")]
public sealed class AdminDashboardController : ControllerBase
{
    private readonly AdminDashboardService _dashboardService;

    public AdminDashboardController(AdminDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Lay so lieu tong quan cho trang Dashboard.
    /// Bao gom: Tong so User, % tang truong (30 ngay), Tong so Voice Matches, Report dang cho duyet, va danh sach User moi nhat.
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetDashboardStats(
        [FromQuery] DateTimeOffset? startDate,
        [FromQuery] DateTimeOffset? endDate,
        CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetDashboardStatsAsync(startDate, endDate, cancellationToken);
        return Ok(new { success = true, data = result });
    }
}
