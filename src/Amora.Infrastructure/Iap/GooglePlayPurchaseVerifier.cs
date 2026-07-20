using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Amora.Application.Abstractions;
using Amora.Application.Iap;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Amora.Infrastructure.Iap;

/// <summary>
/// Google Play — cần service account JSON (GOOGLE_APPLICATION_CREDENTIALS).
/// Production: bật Google Play Android Developer API.
/// </summary>
public sealed class GooglePlayPurchaseVerifier : Amora.Application.Abstractions.IGooglePlayPurchaseVerifier
{
    private const string AndroidPublisherScope = "https://www.googleapis.com/auth/androidpublisher";

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

        // Dev bypass
        if (_options.AllowDevBypass && Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Production")
        {
            return IapVerificationResult.Ok();
        }

        var accessToken = await TryGetAccessTokenAsync(cancellationToken)
            ?? Environment.GetEnvironmentVariable("GOOGLE_PLAY_API_TOKEN");
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            _logger.LogWarning("Google Play credentials not configured — cannot verify Google purchase.");
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

    public async Task<bool> AcknowledgePurchaseAsync(string productId, string token, CancellationToken cancellationToken)
    {
        var packageName = _options.GooglePackageName;
        if (string.IsNullOrWhiteSpace(packageName))
            return false;

        // Dev bypass
        if (_options.AllowDevBypass && Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Production")
        {
            return true;
        }

        var accessToken = await TryGetAccessTokenAsync(cancellationToken)
            ?? Environment.GetEnvironmentVariable("GOOGLE_PLAY_API_TOKEN");
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return false;
        }

        var url =
            $"https://androidpublisher.googleapis.com/androidpublisher/v3/applications/{packageName}/purchases/products/{productId}/tokens/{token}:acknowledge";

        var client = _httpClientFactory.CreateClient("GoogleIap");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.PostAsync(url, null, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    private async Task<string?> TryGetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var credentialsPath = _options.GoogleServiceAccountJsonPath
            ?? Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");

        if (string.IsNullOrWhiteSpace(credentialsPath) || !File.Exists(credentialsPath))
            return null;

        var credential = GoogleCredential.FromFile(credentialsPath).CreateScoped(AndroidPublisherScope);
        if (credential.UnderlyingCredential is not ITokenAccess tokenAccess)
            return null;

        return await tokenAccess.GetAccessTokenForRequestAsync(cancellationToken: cancellationToken);
    }
}
