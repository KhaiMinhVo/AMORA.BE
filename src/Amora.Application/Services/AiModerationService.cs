using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Amora.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Amora.Application.Services;

public sealed class AiModerationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiModerationService> _logger;
    private readonly string? _aiServiceUrl;
    private readonly string? _apiKey;

    public AiModerationService(HttpClient httpClient, IConfiguration configuration, ILogger<AiModerationService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _aiServiceUrl = _configuration["AiService:Url"];
        _apiKey = _configuration["AiService:ApiKey"];
    }

    /// <summary>
    /// Checks if a chat message contains toxic, offensive, or harmful language.
    /// Returns true if the message is toxic, false otherwise.
    /// </summary>
    public async Task<bool> IsMessageToxicAsync(string content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content)) return false;
        if (!IsConfigured())
        {
            _logger.LogWarning("AiService URL or API key is missing. Skipping AI moderation check.");
            return false;
        }

        try
        {
            using var request = CreateRequest("/evaluate", new { text = content });
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
                var doc = JsonDocument.Parse(responseString);
                return doc.RootElement.GetProperty("isToxic").GetBoolean();
            }
            
            _logger.LogError("AiService returned {StatusCode} during toxicity check.", response.StatusCode);
            return false;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while calling AiService for toxicity check.");
            return false;
        }
    }

    /// <summary>
    /// Evaluates a user report and suggests an action.
    /// Returns "Ban", "Ignore", or "Manual" (leave for human admin).
    /// </summary>
    public async Task<AiReportEvaluation> EvaluateReportAsync(string reportedContent, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            return new AiReportEvaluation("Manual", null);
        }

        try
        {
            // Reporter-controlled reason/description must never influence an automated verdict.
            if (string.IsNullOrWhiteSpace(reportedContent))
                return new AiReportEvaluation("Manual", null);

            using var request = CreateRequest("/evaluate", new { text = reportedContent });
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
                var doc = JsonDocument.Parse(responseString);
                bool isToxic = doc.RootElement.GetProperty("isToxic").GetBoolean();
                double score = doc.RootElement.GetProperty("score").GetDouble();
                
                // AI is advisory only. It must never ban or dismiss a user report.
                return new AiReportEvaluation(isToxic ? "Flagged" : "Clear", score);
            }
            
            return new AiReportEvaluation("Manual", null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while calling AiService for report evaluation.");
            return new AiReportEvaluation("Manual", null);
        }
    }

    /// <summary>
    /// Downloads an audio file from the given URL and transcribes it using OpenAI Whisper.
    /// </summary>
    public async Task<string?> TranscribeAudioAsync(string audioUrl, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured() || string.IsNullOrWhiteSpace(audioUrl))
        {
            return null;
        }

        try
        {
            using var request = CreateRequest("/transcribe", new { audioUrl });
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
                var doc = JsonDocument.Parse(responseString);
                return doc.RootElement.GetProperty("text").GetString();
            }

            _logger.LogError("AiService returned {StatusCode} during transcription.", response.StatusCode);
            return null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while calling AiService for transcription.");
            return null;
        }
    }

    private bool IsConfigured()
        => !string.IsNullOrWhiteSpace(_aiServiceUrl)
           && !string.IsNullOrWhiteSpace(_apiKey);

    private HttpRequestMessage CreateRequest(string path, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_aiServiceUrl}{path}")
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Internal-Api-Key", _apiKey);
        return request;
    }
}

public sealed record AiReportEvaluation(string Verdict, double? Score);
