namespace Amora.Application.Dtos.Notifications;

public sealed record RegisterPushTokenRequest(string Token, string DeviceId, string Platform);

public sealed record RemovePushTokenRequest(string DeviceId);
