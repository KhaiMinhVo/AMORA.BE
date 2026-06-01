using System.Net;
using System.Net.Mail;
using Amora.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Amora.Infrastructure.Services;

public sealed class SmtpEmailService : IEmailService
{
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly string _host;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;
    private readonly string _fromEmail;

    public SmtpEmailService(ILogger<SmtpEmailService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _host = configuration["EmailSettings:Host"] ?? "smtp-relay.brevo.com";
        _port = int.TryParse(configuration["EmailSettings:Port"], out var port) ? port : 587;
        _username = configuration["EmailSettings:Username"] ?? "";
        _password = configuration["EmailSettings:Password"] ?? "";
        _fromEmail = configuration["EmailSettings:FromEmail"] ?? "noreply@amora.vn";
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_username) || string.IsNullOrWhiteSpace(_password))
            {
                _logger.LogWarning("SMTP credentials not configured. OTP to {Email}: {Body}", toEmail, body);
                // Return true for local development when credentials are missing
                return true; 
            }

            using var client = new SmtpClient(_host, _port)
            {
                Credentials = new NetworkCredential(_username, _password),
                EnableSsl = true
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_fromEmail, "AMORA Security"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };
            
            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage, cancellationToken);
            _logger.LogInformation("Email sent successfully to {Email}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            return false;
        }
    }
}
