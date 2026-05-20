using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Amora.Infrastructure.Iap;

public sealed class AppleServerNotificationVerifier
{
    private const string JwksUrl = "https://api.storekit.itunes.apple.com/in-app-purchase/publicKeys";
    private static readonly SemaphoreSlim CacheLock = new(1, 1);
    private static JsonWebKeySet? CachedKeys;
    private static DateTimeOffset CacheExpiresAt = DateTimeOffset.MinValue;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AppleServerNotificationVerifier> _logger;

    public AppleServerNotificationVerifier(IHttpClientFactory httpClientFactory, ILogger<AppleServerNotificationVerifier> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<bool> ValidateAsync(string signedPayload, CancellationToken cancellationToken)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.ReadJwtToken(signedPayload);
        var keyId = token.Header.Kid;

        if (string.IsNullOrWhiteSpace(keyId))
            return false;

        var keys = await GetKeysAsync(cancellationToken);
        var signingKey = keys.Keys.FirstOrDefault(k => k.Kid == keyId);
        if (signingKey is null)
        {
            _logger.LogWarning("Apple JWK key not found for kid {Kid}.", keyId);
            return false;
        }

        try
        {
            tokenHandler.ValidateToken(signedPayload, new TokenValidationParameters
            {
                RequireSignedTokens = true,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false,
                IssuerSigningKey = signingKey
            }, out _);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Apple signed payload validation failed.");
            return false;
        }
    }

    private async Task<JsonWebKeySet> GetKeysAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        if (CachedKeys is not null && CacheExpiresAt > now)
            return CachedKeys;

        await CacheLock.WaitAsync(cancellationToken);
        try
        {
            if (CachedKeys is not null && CacheExpiresAt > now)
                return CachedKeys;

            var client = _httpClientFactory.CreateClient("AppleIap");
            var json = await client.GetStringAsync(JwksUrl, cancellationToken);
            CachedKeys = new JsonWebKeySet(json);
            CacheExpiresAt = now.AddHours(12);
            return CachedKeys;
        }
        finally
        {
            CacheLock.Release();
        }
    }
}
