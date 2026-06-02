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

    public AiModerationService(HttpClient httpClient, IConfiguration configuration, ILogger<AiModerationService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _aiServiceUrl = _configuration["AiService:Url"];
    }

    /// <summary>
    /// Checks if a chat message contains toxic, offensive, or harmful language.
    /// Returns true if the message is toxic, false otherwise.
    /// </summary>
    public async Task<bool> IsMessageToxicAsync(string content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content)) return false;
        if (string.IsNullOrWhiteSpace(_aiServiceUrl))
        {
            _logger.LogWarning("AiService URL is missing. Skipping AI moderation check.");
            return false;
        }

        try
        {
            var requestBody = new { text = content };
            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_aiServiceUrl}/evaluate", jsonContent, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
                var doc = JsonDocument.Parse(responseString);
                return doc.RootElement.GetProperty("isToxic").GetBoolean();
            }
            
            _logger.LogError("AiService returned {StatusCode} during toxicity check.", response.StatusCode);
            return false;
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
    public async Task<string> EvaluateReportAsync(UserReport report, string reportedContent, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_aiServiceUrl))
        {
            return "Manual";
        }

        try
        {
            // Evaluate both description and reportedContent
            string fullTextToEvaluate = $"{report.Reason} {report.Description} {reportedContent}";
            
            var requestBody = new { text = fullTextToEvaluate };
            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_aiServiceUrl}/evaluate", jsonContent, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
                var doc = JsonDocument.Parse(responseString);
                bool isToxic = doc.RootElement.GetProperty("isToxic").GetBoolean();
                
                if (isToxic) return "BAN";
                
                // If it's a specific report with evidence, and evidence is not toxic -> Ignore
                if (!string.IsNullOrWhiteSpace(reportedContent))
                {
                     return "IGNORE";
                }
            }
            
            return "Manual";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while calling AiService for report evaluation.");
            return "Manual"; 
        }
    }

    /// <summary>
    /// Downloads an audio file from the given URL and transcribes it using OpenAI Whisper.
    /// </summary>
    public async Task<string?> TranscribeAudioAsync(string audioUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_aiServiceUrl) || string.IsNullOrWhiteSpace(audioUrl))
        {
            return null;
        }

        try
        {
            var requestBody = new { audioUrl = audioUrl };
            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_aiServiceUrl}/transcribe", jsonContent, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
                var doc = JsonDocument.Parse(responseString);
                return doc.RootElement.GetProperty("text").GetString();
            }

            _logger.LogError("AiService returned {StatusCode} during transcription.", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while calling AiService for transcription.");
            return null;
        }
    }
}
