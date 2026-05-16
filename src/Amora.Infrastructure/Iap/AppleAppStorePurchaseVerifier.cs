using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amora.Application.Abstractions;
using Amora.Application.Iap;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Amora.Infrastructure.Iap;

public sealed class AppleAppStorePurchaseVerifier
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IapOptions _options;
    private readonly ILogger<AppleAppStorePurchaseVerifier> _logger;

    public AppleAppStorePurchaseVerifier(
        IHttpClientFactory httpClientFactory,
        IOptions<IapOptions> options,
        ILogger<AppleAppStorePurchaseVerifier> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IapVerificationResult> VerifyAsync(IapVerificationRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.AppleSharedSecret))
            return IapVerificationResult.Fail("Apple shared secret not configured.");

        var client = _httpClientFactory.CreateClient("AppleIap");
        var body = new AppleReceiptBody
        {
            ReceiptData = request.ReceiptOrToken,
            Password = _options.AppleSharedSecret
        };

        foreach (var url in new[] { "https://buy.itunes.apple.com/verifyReceipt", "https://sandbox.itunes.apple.com/verifyReceipt" })
        {
            var response = await client.PostAsJsonAsync(url, body, cancellationToken);
            if (!response.IsSuccessStatusCode) continue;

            var doc = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            var status = doc.GetProperty("status").GetInt32();
            if (status == 21007) continue;
            if (status != 0)
            {
                _logger.LogWarning("Apple verify status {Status}", status);
                return IapVerificationResult.Fail($"Apple verification failed: {status}");
            }

            return IapVerificationResult.Ok();
        }

        return IapVerificationResult.Fail("Apple verification unreachable.");
    }

    private sealed class AppleReceiptBody
    {
        [JsonPropertyName("receipt-data")]
        public string ReceiptData { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;

        [JsonPropertyName("exclude-old-transactions")]
        public bool ExcludeOldTransactions { get; set; } = true;
    }
}
