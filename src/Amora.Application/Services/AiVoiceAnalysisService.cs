using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Amora.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Amora.Application.Services;

public sealed class AiVoiceAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiVoiceAnalysisService> _logger;
    private readonly string? _apiKey;

    public AiVoiceAnalysisService(HttpClient httpClient, IConfiguration configuration, ILogger<AiVoiceAnalysisService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _apiKey = _configuration["GeminiApiKey"];
    }

    public async Task<VoiceTone?> AnalyzeVoiceToneAsync(string audioUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(audioUrl))
        {
            _logger.LogWarning("GeminiApiKey or AudioUrl is missing. Cannot analyze voice tone.");
            return null;
        }

        try
        {
            // 1. Tải file âm thanh từ AudioUrl
            byte[] audioBytes;
            using (var tempClient = new HttpClient())
            {
                audioBytes = await tempClient.GetByteArrayAsync(audioUrl, cancellationToken);
            }

            string base64Audio = Convert.ToBase64String(audioBytes);

            // 2. Gọi Gemini 2.5 Flash
            var prompt = "Hãy nghe đoạn âm thanh này và phân loại sắc thái/âm điệu (Voice Tone) của người nói vào một trong các từ khóa sau: Deep, Gentle, Energetic, Cute, Warm, Husky. Bạn CHỈ được phép trả về đúng 1 từ khóa đó, không giải thích gì thêm.";
            
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = prompt },
                            new 
                            { 
                                inlineData = new 
                                { 
                                    mimeType = "audio/mp3", 
                                    data = base64Audio 
                                } 
                            }
                        }
                    }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";
            
            var response = await _httpClient.PostAsync(url, jsonContent, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(responseString);
                var candidates = doc.RootElement.GetProperty("candidates");
                if (candidates.GetArrayLength() > 0)
                {
                    var resultText = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString()?.Trim();
                    
                    if (Enum.TryParse<VoiceTone>(resultText, true, out var tone))
                    {
                        return tone;
                    }
                }
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Gemini API failed with status {StatusCode}: {Error}", response.StatusCode, error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while analyzing voice tone for {AudioUrl}", audioUrl);
        }

        return null;
    }
}
