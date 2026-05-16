using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Amora.Application.Abstractions;
using Amora.Application.Iap;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Amora.Infrastructure.Iap;

/// <summary>
/// Google Play — cần service account JSON (GOOGLE_APPLICATION_CREDENTIALS).
/// Production: bật Google Play Android Developer API.
/// </summary>
public sealed class GooglePlayPurchaseVerifier
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IapOptions _options;
    private readonly ILogger<GooglePlayPurchaseVerifier> _logger;

    public GooglePlayPurchaseVerifier(
        IHttpClientFactory httpClientFactory,
        IOptions<IapOptions> options,
        ILogger<GooglePlayPurchaseVerifier> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IapVerificationResult> VerifyAsync(IapVerificationRequest request, CancellationToken cancellationToken)
    {
        var packageName = _options.GooglePackageName;
        if (string.IsNullOrWhiteSpace(packageName))
            return IapVerificationResult.Fail("Google package name not configured.");

        // TODO: OAuth2 service account token — stub kiểm tra token không rỗng
        var accessToken = Environment.GetEnvironmentVariable("GOOGLE_PLAY_API_TOKEN");
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            _logger.LogWarning("GOOGLE_PLAY_API_TOKEN not set — cannot verify Google purchase in production.");
            return IapVerificationResult.Fail("Google Play API not configured.");
        }

        var url =
            $"https://androidpublisher.googleapis.com/androidpublisher/v3/applications/{packageName}/purchases/products/{request.ProductId}/tokens/{request.ReceiptOrToken}";

        var client = _httpClientFactory.CreateClient("GoogleIap");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return IapVerificationResult.Fail("Google purchase verification failed.");

        var doc = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        if (doc.TryGetProperty("purchaseState", out var state) && state.GetInt32() != 0)
            return IapVerificationResult.Fail("Purchase not completed.");

        return IapVerificationResult.Ok();
    }
}
