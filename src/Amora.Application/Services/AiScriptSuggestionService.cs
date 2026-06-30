using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Amora.Application.Services;

public sealed class AiScriptSuggestionService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiScriptSuggestionService> _logger;
    private readonly string? _apiKey;

    public AiScriptSuggestionService(HttpClient httpClient, IConfiguration configuration, ILogger<AiScriptSuggestionService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _apiKey = _configuration["Gemini:ApiKey"];
    }

    public async Task<List<string>> GenerateVoiceIntroSuggestionsAsync(string displayName, string? bio, string? interests, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogError("Gemini API Key is missing.");
            throw new InvalidOperationException("Chưa cấu hình Gemini API Key. Vui lòng thiết lập biến môi trường GEMINI_API_KEY hoặc cấu hình trong appsettings.json.");
        }

        string prompt = $"Bạn là một chuyên gia tư vấn hẹn hò. Hãy viết 3 đoạn kịch bản giới thiệu bản thân ngắn gọn (khoảng 30-50 từ, đọc mất 10-30 giây) để dùng làm audio intro trên ứng dụng hẹn hò.\n" +
                        $"Tên người dùng: {displayName}\n";
        
        if (!string.IsNullOrWhiteSpace(bio))
        {
            prompt += $"Tiểu sử (Bio): {bio}\n";
        }
        if (!string.IsNullOrWhiteSpace(interests))
        {
            prompt += $"Sở thích: {interests}\n";
        }
        
        prompt += "\nYêu cầu:\n" +
                  "- Giọng điệu tự nhiên, vui vẻ, LỊCH SỰ và TRƯỞNG THÀNH. Có thể thả thính nhưng phải tinh tế.\n" +
                  "- Tuyệt đối KHÔNG dùng từ lóng, KHÔNG thô tục, KHÔNG suồng sã hay sến súa.\n" +
                  "- Nội dung phải là một câu hoàn chỉnh, trôi chảy, văn minh và tôn trọng người nghe.\n" +
                  "- Mỗi kịch bản là một dòng riêng biệt, bắt đầu bằng dấu gạch ngang (-).\n" +
                  "- Tuyệt đối không viết lan man, không thêm text giải thích.";

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
            },
            generationConfig = new
            {
                temperature = 0.7,
                topP = 0.9,
                topK = 40,
                maxOutputTokens = 1024
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync($"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash:generateContent?key={_apiKey}", content, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(responseString);
                
                var candidates = doc.RootElement.GetProperty("candidates");
                if (candidates.GetArrayLength() > 0)
                {
                    var text = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
                    
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                        var suggestions = new List<string>();
                        foreach (var line in lines)
                        {
                            var cleanLine = line.Trim().TrimStart('-', '*', ' ');
                            if (!string.IsNullOrWhiteSpace(cleanLine) && cleanLine.Length > 10)
                            {
                                suggestions.Add(cleanLine);
                            }
                        }
                        
                        if (suggestions.Count > 0)
                        {
                            return suggestions;
                        }
                    }
                }
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Gemini API failed with status code {StatusCode}. Error: {Error}", response.StatusCode, error);
                throw new InvalidOperationException($"Lỗi khi gọi Gemini API: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while calling Gemini API.");
            throw;
        }

        throw new InvalidOperationException("AI không tạo ra kịch bản hợp lệ.");
    }

}
