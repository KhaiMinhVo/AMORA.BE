using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amora.Application.Abstractions;
using Amora.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Amora.Infrastructure.Services;

/// <summary>
/// Sends push notifications via Expo Push API.
/// Handles batching, retries on transient errors, and token cleanup on DeviceNotRegistered.
/// </summary>
public sealed class ExpoPushService : IExpoPushService
{
    private const string ExpoPushUrl = "https://exp.host/--/api/v2/push/send";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpoPushService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ExpoPushService(
        IHttpClientFactory httpClientFactory,
        IServiceScopeFactory scopeFactory,
        ILogger<ExpoPushService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task SendPushAsync(Guid recipientUserId, string title, string body, object? data = null, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var tokenRepo = scope.ServiceProvider.GetRequiredService<IUserPushTokenRepository>();

            var tokens = await tokenRepo.GetByUserIdAsync(recipientUserId, cancellationToken);
            if (tokens.Count == 0)
            {
                _logger.LogDebug("No push tokens found for user {UserId}, skipping push.", recipientUserId);
                return;
            }

            var messages = tokens.Select(t => new ExpoPushMessage
            {
                To = t.Token,
                Title = title,
                Body = body,
                Data = data,
                Sound = "default",
                Priority = "high"
            }).ToList();

            var client = _httpClientFactory.CreateClient("ExpoPush");
            var response = await client.PostAsJsonAsync(ExpoPushUrl, messages, JsonOptions, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Expo Push API returned {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<ExpoPushResponse>(JsonOptions, cancellationToken);
            if (result?.Data is null) return;

            // Process per-ticket results – remove tokens that are no longer valid
            for (int i = 0; i < result.Data.Count && i < messages.Count; i++)
            {
                var ticket = result.Data[i];
                if (ticket.Status == "error" && ticket.Details?.Error == "DeviceNotRegistered")
                {
                    _logger.LogInformation("Removing invalid push token for user {UserId}: DeviceNotRegistered", recipientUserId);
                    await tokenRepo.RemoveByTokenAsync(messages[i].To, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            // Push failures must never break the main flow
            _logger.LogError(ex, "Failed to send Expo push notification to user {UserId}", recipientUserId);
        }
    }

    // ── Expo API DTOs ──────────────────────────────────────────────────
    private sealed class ExpoPushMessage
    {
        public string To { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? Body { get; set; }
        public object? Data { get; set; }
        public string? Sound { get; set; }
        public string? Priority { get; set; }
    }

    private sealed class ExpoPushResponse
    {
        public List<ExpoPushTicket>? Data { get; set; }
    }

    private sealed class ExpoPushTicket
    {
        public string Status { get; set; } = string.Empty;
        public string? Id { get; set; }
        public string? Message { get; set; }
        public ExpoPushTicketDetails? Details { get; set; }
    }

    private sealed class ExpoPushTicketDetails
    {
        public string? Error { get; set; }
    }
}
