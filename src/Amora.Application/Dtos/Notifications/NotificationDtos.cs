using Amora.Domain.Enums;

namespace Amora.Application.Dtos.Notifications;

public sealed record NotificationDto(
    Guid Id,
    NotificationType Type,
    string Title,
    string Body,
    bool IsRead,
    string? DataJson,
    DateTimeOffset CreatedAt);

public sealed record UnreadCountDto(int Count);
