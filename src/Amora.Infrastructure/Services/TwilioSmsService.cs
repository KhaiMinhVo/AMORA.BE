using Amora.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Amora.Infrastructure.Services;

public class TwilioSmsService : ISmsService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TwilioSmsService> _logger;

    public TwilioSmsService(IConfiguration configuration, ILogger<TwilioSmsService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        var accountSid = _configuration["Twilio:AccountSid"];
        var authToken = _configuration["Twilio:AuthToken"];
        var fromNumber = _configuration["Twilio:FromNumber"];

        if (string.IsNullOrWhiteSpace(accountSid) || string.IsNullOrWhiteSpace(authToken) || string.IsNullOrWhiteSpace(fromNumber))
        {
            _logger.LogWarning("Twilio configuration is missing. Mocking SMS sending.");
            _logger.LogInformation("MOCK SMS to {Phone}: {Message}", phoneNumber, message);
            return true;
        }

        try
        {
            TwilioClient.Init(accountSid, authToken);

            var messageOptions = new CreateMessageOptions(new PhoneNumber(phoneNumber))
            {
                From = new PhoneNumber(fromNumber),
                Body = message
            };

            var msg = await MessageResource.CreateAsync(messageOptions);
            _logger.LogInformation("Twilio SMS sent to {Phone}. SID: {Sid}", phoneNumber, msg.Sid);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {Phone} via Twilio.", phoneNumber);
            return false;
        }
    }
}
