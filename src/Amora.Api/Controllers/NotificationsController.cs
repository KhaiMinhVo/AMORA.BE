using Amora.Application.Common;
using Amora.Application.Dtos.Notifications;
using Amora.Application.Services;
using Amora.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Amora.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly NotificationService _notificationService;
    private readonly ICurrentUserService _currentUser;

    public NotificationsController(NotificationService notificationService, ICurrentUserService currentUser)
    {
        _notificationService = notificationService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Lấy danh sách thông báo của user (phân trang)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<NotificationDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<NotificationDto>>>> GetNotifications(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20, 
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 50) pageSize = 20;

        var result = await _notificationService.GetUserNotificationsAsync(_currentUser.UserId, page, pageSize, cancellationToken);
        return Ok(ApiResponse<PagedResult<NotificationDto>>.Ok(result));
    }

    /// <summary>
    /// Lấy số lượng thông báo chưa đọc
    /// </summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(ApiResponse<UnreadCountDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UnreadCountDto>>> GetUnreadCount(CancellationToken cancellationToken = default)
    {
        var result = await _notificationService.GetUnreadCountAsync(_currentUser.UserId, cancellationToken);
        return Ok(ApiResponse<UnreadCountDto>.Ok(result));
    }

    /// <summary>
    /// Đánh dấu 1 thông báo là đã đọc
    /// </summary>
    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken = default)
    {
        await _notificationService.MarkAsReadAsync(id, _currentUser.UserId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Marked as read successfully."));
    }

    /// <summary>
    /// Đánh dấu tất cả thông báo là đã đọc
    /// </summary>
    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken = default)
    {
        await _notificationService.MarkAllAsReadAsync(_currentUser.UserId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "All marked as read successfully."));
    }

    /// <summary>
    /// Xóa 1 thông báo
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteNotification(Guid id, CancellationToken cancellationToken = default)
    {
        await _notificationService.DeleteAsync(id, _currentUser.UserId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Deleted successfully."));
    }

    // ── Push Token Management ──────────────────────────────────────────

    /// <summary>
    /// Lưu hoặc cập nhật Expo Push Token cho thiết bị hiện tại.
    /// </summary>
    [HttpPut("push-token")]
    public async Task<IActionResult> RegisterPushToken([FromBody] RegisterPushTokenRequest request, CancellationToken cancellationToken = default)
    {
        await _notificationService.RegisterPushTokenAsync(
            _currentUser.UserId, 
            request.Token, 
            request.DeviceId, 
            request.Platform, 
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, "Push token registered."));
    }

    /// <summary>
    /// Xóa Expo Push Token khi đăng xuất.
    /// </summary>
    [HttpDelete("push-token")]
    public async Task<IActionResult> RemovePushToken([FromBody] RemovePushTokenRequest request, CancellationToken cancellationToken = default)
    {
        await _notificationService.RemovePushTokenAsync(
            _currentUser.UserId, 
            request.DeviceId, 
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, "Push token removed."));
    }
}
