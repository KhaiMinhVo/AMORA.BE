using Amora.Application.Common;
using Amora.Application.Dtos.Notifications;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Amora.Application.Services;

public sealed class NotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(INotificationRepository notificationRepository, ILogger<NotificationService> logger)
    {
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    public async Task<PagedResult<NotificationDto>> GetUserNotificationsAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _notificationRepository.GetByUserIdAsync(userId, page, pageSize, cancellationToken);
        
        var dtos = items.Select(x => new NotificationDto(
            x.Id,
            x.Type,
            x.Title,
            x.Body,
            x.IsRead,
            x.DataJson,
            x.CreatedAt
        )).ToList();

        return new PagedResult<NotificationDto> { Items = dtos, TotalCount = totalCount };
    }

    public async Task<UnreadCountDto> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var count = await _notificationRepository.GetUnreadCountAsync(userId, cancellationToken);
        return new UnreadCountDto(count);
    }

    public async Task MarkAsReadAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        await _notificationRepository.MarkAsReadAsync(id, userId, cancellationToken);
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _notificationRepository.MarkAllAsReadAsync(userId, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        await _notificationRepository.DeleteAsync(id, userId, cancellationToken);
    }

    public async Task SendNotificationAsync(Guid userId, NotificationType type, string title, string body, string? dataJson = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = type,
                Title = title,
                Body = body,
                IsRead = false,
                DataJson = dataJson,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _notificationRepository.AddAsync(notification, cancellationToken);
            await _notificationRepository.SaveChangesAsync(cancellationToken);
            
            // TODO: In the future, we can inject a real-time service here (like SignalR Hub or FCM)
            // _realtimeNotifier.SendNotification(userId, notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to user {UserId}. Type: {Type}", userId, type);
        }
    }
}
