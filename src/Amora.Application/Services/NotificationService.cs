using Amora.Application.Abstractions;
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
    private readonly IRealtimeNotifier _realtimeNotifier;
    private readonly IExpoPushService _expoPushService;
    private readonly IUserPushTokenRepository _pushTokenRepository;

    public NotificationService(
        INotificationRepository notificationRepository, 
        ILogger<NotificationService> logger,
        IRealtimeNotifier realtimeNotifier,
        IExpoPushService expoPushService,
        IUserPushTokenRepository pushTokenRepository)
    {
        _notificationRepository = notificationRepository;
        _logger = logger;
        _realtimeNotifier = realtimeNotifier;
        _expoPushService = expoPushService;
        _pushTokenRepository = pushTokenRepository;
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

    /// <summary>
    /// Gửi notification cho user: lưu DB → SignalR → Expo Push.
    /// actorId: người tạo hành động (nếu trùng userId thì skip, không gửi cho chính mình).
    /// </summary>
    public async Task SendNotificationAsync(
        Guid userId, 
        NotificationType type, 
        string title, 
        string body, 
        string? dataJson = null, 
        CancellationToken cancellationToken = default,
        Guid? actorId = null)
    {
        try
        {
            // Không gửi cho chính người tạo hành động
            if (actorId.HasValue && actorId.Value == userId)
            {
                _logger.LogDebug("Skipping notification for user {UserId} because they are the actor.", userId);
                return;
            }

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
            
            // SignalR (khi app đang mở)
            await _realtimeNotifier.NotifySystemNotificationAsync(userId, notification, cancellationToken);

            // Expo Push (khi app đóng hoặc ở background)
            object? pushData = null;
            if (!string.IsNullOrWhiteSpace(dataJson))
            {
                try
                {
                    pushData = System.Text.Json.JsonSerializer.Deserialize<object>(dataJson);
                }
                catch
                {
                    pushData = new { raw = dataJson };
                }
            }

            await _expoPushService.SendPushAsync(userId, title, body, pushData, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to user {UserId}. Type: {Type}", userId, type);
        }
    }

    // ── Push Token Management ──────────────────────────────────────────

    public async Task RegisterPushTokenAsync(Guid userId, string token, string deviceId, string platform, CancellationToken cancellationToken = default)
    {
        var pushToken = new UserPushToken
        {
            UserId = userId,
            Token = token,
            DeviceId = deviceId,
            Platform = platform
        };

        await _pushTokenRepository.AddOrUpdateAsync(pushToken, cancellationToken);
    }

    public async Task RemovePushTokenAsync(Guid userId, string deviceId, CancellationToken cancellationToken = default)
    {
        await _pushTokenRepository.RemoveByDeviceAsync(userId, deviceId, cancellationToken);
    }
}
