using Amora.Application.Iap;
using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Amora.Infrastructure.Iap;

public sealed class GoogleWebhookTokenValidator
{
    private readonly IapOptions _options;
    private readonly ILogger<GoogleWebhookTokenValidator> _logger;

    public GoogleWebhookTokenValidator(IOptions<IapOptions> options, ILogger<GoogleWebhookTokenValidator> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> ValidateAsync(string jwt, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.GoogleWebhookAudience))
            return true;

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(jwt);

            var audience = payload.Audience as string;
            if (!string.IsNullOrWhiteSpace(_options.GoogleWebhookAudience)
                && !string.Equals(audience, _options.GoogleWebhookAudience, StringComparison.Ordinal))
            {
                _logger.LogWarning("Google webhook token audience mismatch: {Audience}.", payload.Audience);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(_options.GoogleWebhookServiceAccountEmail)
                && !string.Equals(payload.Email, _options.GoogleWebhookServiceAccountEmail, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Google webhook token email mismatch: {Email}.", payload.Email);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Google webhook token validation failed.");
            return false;
        }
    }
}
