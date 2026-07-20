using Amora.Application.Abstractions;
using Amora.Application.Iap;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Amora.Infrastructure.Iap;

/// <summary>Định tuyến Apple / Google; dev bypass khi AllowDevBypass=true.</summary>
public sealed class CompositeInAppPurchaseVerifier : IInAppPurchaseVerifier
{
    private readonly AppleAppStorePurchaseVerifier _apple;
    private readonly IGooglePlayPurchaseVerifier _google;
    private readonly IapOptions _options;
    private readonly ILogger<CompositeInAppPurchaseVerifier> _logger;

    public CompositeInAppPurchaseVerifier(
        AppleAppStorePurchaseVerifier apple,
        IGooglePlayPurchaseVerifier google,
        IOptions<IapOptions> options,
        ILogger<CompositeInAppPurchaseVerifier> logger)
    {
        _apple = apple;
        _google = google;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IapVerificationResult> VerifyAsync(IapVerificationRequest request, CancellationToken cancellationToken = default)
    {
        if (_options.AllowDevBypass && request.ReceiptOrToken == "dev-bypass")
        {
            _logger.LogWarning("IAP dev-bypass used for product {ProductId}", request.ProductId);
            return IapVerificationResult.Ok();
        }

        return request.Platform.ToLowerInvariant() switch
        {
            "apple" or "ios" => await _apple.VerifyAsync(request, cancellationToken),
            "google" or "android" => await _google.VerifyAsync(request, cancellationToken),
            _ => IapVerificationResult.Fail($"Unsupported platform: {request.Platform}")
        };
    }
}
