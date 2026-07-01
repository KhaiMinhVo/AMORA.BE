using System.Text;
using System.Text.Json;
using Amora.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Amora.Application.Services;

public sealed class AiEmotionAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly ILogger<AiEmotionAnalysisService> _logger;

    public AiEmotionAnalysisService(HttpClient httpClient, IConfiguration configuration, ILogger<AiEmotionAnalysisService> logger)
    {
        _httpClient = httpClient;
        _apiKey = configuration["GeminiApiKey"];
        _logger = logger;
    }

    public async Task<PetEmotion> AnalyzeConversationEmotionAsync(string conversationText, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("GeminiApiKey is missing. Cannot analyze emotion. Returning Neutral.");
            return PetEmotion.Neutral;
        }

        if (string.IsNullOrWhiteSpace(conversationText))
            return PetEmotion.Neutral;

        try
        {
            var prompt = $@"Bạn là một chuyên gia phân tích tâm lý. Dựa vào cuộc hội thoại sau, hãy phân tích cảm xúc chung và trả về 1 trong các từ khóa sau tương ứng với cảm xúc của cuộc hội thoại: Neutral, Happy, Sad. Chỉ trả về đúng 1 từ khóa đó, không trả về thêm bất kỳ văn bản nào khác.

Cuộc hội thoại:
{conversationText}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash:generateContent?key={_apiKey}";
            
            var response = await _httpClient.PostAsync(url, jsonContent, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(responseString);
                
                if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    if (candidates[0].TryGetProperty("content", out var content) && 
                        content.TryGetProperty("parts", out var parts) && 
                        parts.GetArrayLength() > 0)
                    {
                        var text = parts[0].GetProperty("text").GetString()?.Trim();
                        if (Enum.TryParse<PetEmotion>(text, true, out var emotion))
                        {
                            return emotion;
                        }
                        
                        _logger.LogWarning("AiEmotionAnalysisService could not parse response: {Text}", text);
                    }
                }
            }
            else
            {
                _logger.LogError("AiEmotionAnalysisService API returned status {Status}: {Content}", response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AiEmotionAnalysisService encountered an error.");
        }

        return PetEmotion.Neutral;
    }
}
