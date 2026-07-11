using Amora.Application.Dtos.Admin;
using Amora.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Amora.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public sealed class AdminModerationController : ControllerBase
{
    private readonly AdminModerationService _adminService;

    public AdminModerationController(AdminModerationService adminService)
    {
        _adminService = adminService;
    }

    /// <summary>
    /// Lấy danh sách toàn bộ users trong hệ thống (có phân trang và tìm kiếm)
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20, 
        [FromQuery] string? keyword = null, 
        [FromQuery] string? subscriptionType = null, 
        [FromQuery] bool? isBanned = null, 
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var result = await _adminService.GetUsersAsync(page, pageSize, keyword, subscriptionType, isBanned, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Tạo tài khoản Admin mới
    /// </summary>
    [HttpPost("users/create-admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminRequest request, CancellationToken cancellationToken = default)
    {
        await _adminService.CreateAdminAsync(request, cancellationToken);
        return Ok(new { message = "Admin account created successfully." });
    }

    /// <summary>
    /// Lấy thông tin chi tiết của một user (dùng cho trang chi tiết người dùng)
    /// </summary>
    [HttpGet("users/{userId:guid}")]
    public async Task<IActionResult> GetUser(Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await _adminService.GetUserDetailsAsync(userId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách các báo cáo vi phạm
    /// </summary>
    [HttpGet("reports")]
    public async Task<IActionResult> GetReports([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var result = await _adminService.GetReportsAsync(page, pageSize, status, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Xử lý một báo cáo (Ignore, Warning, Ban)
    /// </summary>
    [HttpPost("reports/{reportId:guid}/resolve")]
    public async Task<IActionResult> ResolveReport(Guid reportId, [FromBody] ResolveReportRequest request, CancellationToken cancellationToken = default)
    {
        await _adminService.ResolveReportAsync(reportId, request, cancellationToken);
        return Ok(new { message = "Report resolved successfully." });
    }

    /// <summary>
    /// Ban user trực tiếp
    /// </summary>
    [HttpPost("users/{userId:guid}/ban")]
    public async Task<IActionResult> BanUser(Guid userId, [FromBody] BanUserRequest request, CancellationToken cancellationToken = default)
    {
        await _adminService.BanUserAsync(userId, request, cancellationToken);
        return Ok(new { message = "User has been banned." });
    }

    /// <summary>
    /// Bỏ cấm người dùng
    /// </summary>
    [HttpPost("users/{userId:guid}/unban")]
    public async Task<IActionResult> UnbanUser(Guid userId, CancellationToken cancellationToken = default)
    {
        await _adminService.UnbanUserAsync(userId, cancellationToken);
        return Ok(new { message = "User has been unbanned successfully." });
    }

    /// <summary>
    /// Hoàn/Cộng kim cương cho người dùng
    /// </summary>
    [HttpPost("users/{userId:guid}/refund-diamonds")]
    public async Task<IActionResult> RefundDiamonds(Guid userId, [FromBody] RefundDiamondsRequest request, CancellationToken cancellationToken = default)
    {
        await _adminService.RefundDiamondsAsync(userId, request.Amount, request.Reason, cancellationToken);
        return Ok(new { message = $"Đã hoàn {request.Amount} kim cương cho người dùng thành công." });
    }

    /// <summary>
    /// Lấy danh sách các tài khoản đang khiếu nại (Appeal)
    /// </summary>
    [HttpGet("appeals")]
    public async Task<IActionResult> GetAppeals([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var result = await _adminService.GetPendingAppealsAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Xử lý đơn khiếu nại (Approve/Reject)
    /// </summary>
    [HttpPost("appeals/{userId:guid}/resolve")]
    public async Task<IActionResult> ResolveAppeal(Guid userId, [FromBody] ResolveAppealRequest request, CancellationToken cancellationToken = default)
    {
        await _adminService.ResolveAppealAsync(userId, request, cancellationToken);
        return Ok(new { message = "Appeal resolved successfully." });
    }
    /// <summary>
    /// Lấy thống kê kiểm duyệt (số lượng báo cáo, tài khoản bị khoá)
    /// </summary>
    [HttpGet("moderation/stats")]
    public async Task<IActionResult> GetModerationStats(CancellationToken cancellationToken = default)
    {
        var result = await _adminService.GetModerationStatsAsync(cancellationToken);
        return Ok(result);
    }
}
