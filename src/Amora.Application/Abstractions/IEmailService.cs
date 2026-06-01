namespace Amora.Application.Abstractions;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default);
}
