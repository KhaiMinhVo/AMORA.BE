using Amora.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Amora.Api.Controllers;

[ApiController]
[Route("api/admin/notifications")]
[Authorize(Roles = "Admin")]
public sealed class AdminNotificationsController : ControllerBase
{
    private readonly IAdminNotificationRepository _notificationRepository;

    public AdminNotificationsController(IAdminNotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var (items, totalCount) = await _notificationRepository.GetPagedAsync(page, pageSize, cancellationToken);

        return Ok(new
        {
            Items = items.Select(x => new
            {
                Id = x.Id,
                Type = x.Type.ToString(),
                Title = x.Title,
                Message = x.Message,
                ActionUrl = x.ActionUrl,
                CreatedAt = x.CreatedAt,
                IsRead = x.IsRead
            }),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        });
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        var count = await _notificationRepository.GetUnreadCountAsync(cancellationToken);
        return Ok(new { Count = count });
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        await _notificationRepository.MarkAsReadAsync(id, cancellationToken);
        return Ok(new { Success = true });
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        await _notificationRepository.MarkAllAsReadAsync(cancellationToken);
        return Ok(new { Success = true });
    }
}
