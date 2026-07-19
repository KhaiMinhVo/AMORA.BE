namespace Amora.Application.Abstractions;

/// <summary>
/// Abstraction for sending push notifications via Expo Push API.
/// </summary>
public interface IExpoPushService
{
    Task SendPushAsync(Guid recipientUserId, string title, string body, object? data = null, CancellationToken cancellationToken = default);
}
