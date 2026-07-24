using Amora.Application.Dtos.Posts;
using Amora.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace Amora.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
[AllowAnonymous] // Webhook từ internal service, xác thực bằng secret header thay vì JWT
public sealed class WebhookController : ControllerBase
{
    private readonly AudioProcessingService _audioProcessingService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(
        AudioProcessingService audioProcessingService,
        IConfiguration configuration,
        ILogger<WebhookController> logger)
    {
        _audioProcessingService = audioProcessingService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Python Worker goi endpoint nay sau khi xu ly am thanh xong.
    /// Xac thuc bang shared secret truoc khi cap nhat ket qua.
    /// </summary>
    [HttpPost("audio-processed")]
    public async Task<IActionResult> HandleAudioProcessed(
        [FromBody] AudioProcessedPayload payload,
        CancellationToken cancellationToken)
    {
        // Xác thực nguồn gốc webhook bằng shared secret
        var expectedSecret = _configuration["Webhooks:Secret"];
        if (string.IsNullOrWhiteSpace(expectedSecret))
        {
            _logger.LogCritical("[Webhook] Webhooks:Secret is not configured.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                success = false,
                message = "Webhook authentication is unavailable."
            });
        }

        if (!Request.Headers.TryGetValue("X-Webhook-Secret", out var receivedSecret)
            || !SecretsMatch(receivedSecret.ToString(), expectedSecret))
        {
            _logger.LogWarning("[Webhook] Unauthorized webhook call for PostId={PostId}", payload.PostId);
            return Unauthorized(new { success = false, message = "Invalid webhook secret." });
        }

        _logger.LogInformation("[Webhook] Nhận kết quả từ Worker: PostId={PostId}, Status={Status}",
            payload.PostId, payload.Status);

        await _audioProcessingService.HandleAudioProcessedAsync(payload, cancellationToken);

        return Ok(new { success = true });
    }

    private static bool SecretsMatch(string actual, string expected)
    {
        var actualBytes = Encoding.UTF8.GetBytes(actual);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        return actualBytes.Length == expectedBytes.Length
               && CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
    }
}
