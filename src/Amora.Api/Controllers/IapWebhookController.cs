using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using Amora.Application.Iap;
using Amora.Infrastructure.Iap;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Amora.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/iap/webhooks")]
public sealed class IapWebhookController : ControllerBase
{
    private readonly IapWebhookService _iapWebhookService;
    private readonly AppleServerNotificationVerifier _appleVerifier;
    private readonly GoogleWebhookTokenValidator _googleValidator;
    private readonly IapOptions _options;
    private readonly ILogger<IapWebhookController> _logger;

    public IapWebhookController(
        IapWebhookService iapWebhookService,
        AppleServerNotificationVerifier appleVerifier,
        GoogleWebhookTokenValidator googleValidator,
        IOptions<IapOptions> options,
        ILogger<IapWebhookController> logger)
    {
        _iapWebhookService = iapWebhookService;
        _appleVerifier = appleVerifier;
        _googleValidator = googleValidator;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Xu ly Apple Server Notifications cho IAP (refund/renewal).
    /// Cap nhat trang thai giao dich theo payload da ky.
    /// </summary>
    [HttpPost("apple")]
    public async Task<IActionResult> Apple([FromBody] AppleServerNotificationRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SignedPayload))
            return BadRequest(new { success = false, message = "Missing signedPayload." });

        if (!await _appleVerifier.ValidateAsync(request.SignedPayload, cancellationToken))
            return Unauthorized(new { success = false, message = "Invalid Apple signature." });

        var payload = TryParseApplePayload(request.SignedPayload);
        if (payload is null)
            return BadRequest(new { success = false, message = "Invalid Apple payload." });

        if (!string.IsNullOrWhiteSpace(_options.AppleBundleId)
            && !string.Equals(payload.BundleId, _options.AppleBundleId, StringComparison.Ordinal))
        {
            return Unauthorized(new { success = false, message = "Apple bundleId mismatch." });
        }

        if (IsAppleRefund(payload.NotificationType))
        {
            await _iapWebhookService.HandleRefundAsync(_options.ApplePlatform, payload.TransactionId, payload.NotificationType, cancellationToken);
        }
        else if (IsAppleRenewal(payload.NotificationType))
        {
            await _iapWebhookService.HandleRenewalAsync(_options.ApplePlatform, payload.TransactionId, cancellationToken);
        }

        return Ok(new { success = true });
    }

    /// <summary>
    /// Xu ly Google Play Pub/Sub webhook cho IAP (refund/renewal).
    /// Cap nhat trang thai giao dich tu thong bao Pub/Sub.
    /// </summary>
    [HttpPost("google")]
    public async Task<IActionResult> Google([FromBody] GooglePubSubEnvelope envelope, CancellationToken cancellationToken)
    {
        if (!await ValidateGoogleAuthorizationAsync(cancellationToken))
            return Unauthorized(new { success = false, message = "Invalid Google authorization." });

        if (string.IsNullOrWhiteSpace(envelope.Message?.Data))
            return BadRequest(new { success = false, message = "Missing Pub/Sub message data." });

        string payloadJson;
        try
        {
            payloadJson = Encoding.UTF8.GetString(Convert.FromBase64String(envelope.Message.Data));
        }
        catch (FormatException)
        {
            return BadRequest(new { success = false, message = "Invalid Pub/Sub message data." });
        }

        var notification = TryParseGooglePayload(payloadJson);
        if (notification is null)
            return BadRequest(new { success = false, message = "Invalid Google payload." });

        if (notification.IsRefund)
        {
            await _iapWebhookService.HandleRefundAsync(_options.GooglePlatform, notification.TransactionId, notification.NotificationType.ToString(), cancellationToken);
        }
        else if (notification.IsRenewal)
        {
            await _iapWebhookService.HandleRenewalAsync(_options.GooglePlatform, notification.TransactionId, cancellationToken);
        }

        return Ok(new { success = true });
    }

    private async Task<bool> ValidateGoogleAuthorizationAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.GoogleWebhookAudience))
            return true;

        var authHeader = Request.Headers.Authorization.ToString();
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return false;

        var token = authHeader["Bearer ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(token))
            return false;

        return await _googleValidator.ValidateAsync(token, cancellationToken);
    }

    private static bool IsAppleRefund(string? notificationType)
        => notificationType is "REFUND" or "REVOKE";

    private static bool IsAppleRenewal(string? notificationType)
        => notificationType is "DID_RENEW" or "SUBSCRIBED" or "DID_RECOVER";

    private static AppleNotificationPayload? TryParseApplePayload(string signedPayload)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var notificationToken = handler.ReadJwtToken(signedPayload);

            var notificationType = notificationToken.Payload.TryGetValue("notificationType", out var typeValue)
                ? typeValue?.ToString()
                : null;

            if (!notificationToken.Payload.TryGetValue("data", out var dataValue) || dataValue is not JsonElement dataElement)
                return null;

            if (!dataElement.TryGetProperty("signedTransactionInfo", out var signedTxElement))
                return null;

            var signedTransactionInfo = signedTxElement.GetString();
            if (string.IsNullOrWhiteSpace(signedTransactionInfo))
                return null;

            var transactionToken = handler.ReadJwtToken(signedTransactionInfo);
            var transactionId = transactionToken.Payload.TryGetValue("transactionId", out var txValue)
                ? txValue?.ToString()
                : null;

            var originalTransactionId = transactionToken.Payload.TryGetValue("originalTransactionId", out var originalValue)
                ? originalValue?.ToString()
                : null;

            var bundleId = transactionToken.Payload.TryGetValue("bundleId", out var bundleValue)
                ? bundleValue?.ToString()
                : null;

            var effectiveTransactionId = transactionId ?? originalTransactionId;
            if (string.IsNullOrWhiteSpace(effectiveTransactionId))
                return null;

            return new AppleNotificationPayload(effectiveTransactionId, bundleId, notificationType);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static GoogleNotificationPayload? TryParseGooglePayload(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("subscriptionNotification", out var subscription))
            {
                var token = subscription.GetProperty("purchaseToken").GetString();
                var type = subscription.GetProperty("notificationType").GetInt32();

                if (string.IsNullOrWhiteSpace(token))
                    return null;

                return new GoogleNotificationPayload(token, type, true);
            }

            if (root.TryGetProperty("oneTimeProductNotification", out var oneTime))
            {
                var token = oneTime.GetProperty("purchaseToken").GetString();
                var type = oneTime.GetProperty("notificationType").GetInt32();

                if (string.IsNullOrWhiteSpace(token))
                    return null;

                return new GoogleNotificationPayload(token, type, false);
            }
        }
        catch (Exception)
        {
            return null;
        }

        return null;
    }

    private sealed record AppleNotificationPayload(string TransactionId, string? BundleId, string? NotificationType);

    private sealed record GoogleNotificationPayload(string TransactionId, int NotificationType, bool IsSubscription)
    {
        public bool IsRefund => IsSubscription
            ? NotificationType == 12
            : NotificationType == 2;

        public bool IsRenewal => IsSubscription && NotificationType == 2;
    }

    public sealed class AppleServerNotificationRequest
    {
        public string? SignedPayload { get; init; }
    }

    public sealed class GooglePubSubEnvelope
    {
        public GooglePubSubMessage? Message { get; init; }
    }

    public sealed class GooglePubSubMessage
    {
        public string? Data { get; init; }
        public string? MessageId { get; init; }
        public string? PublishTime { get; init; }
    }
}
