namespace Amora.Application.Abstractions;

public interface ISmsService
{
    Task<bool> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
}
