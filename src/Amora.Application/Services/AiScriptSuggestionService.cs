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
            _logger.LogWarning("Gemini API Key is missing. Returning fallback suggestions.");
            return GetFallbackSuggestions();
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
                  "- Giọng điệu tự nhiên, vui vẻ, có chút thả thính nhẹ nhàng.\n" +
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
            }
        };

        try
        {
            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}", jsonContent, cancellationToken);
            
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
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while calling Gemini API.");
        }

        return GetFallbackSuggestions();
    }

    private List<string> GetFallbackSuggestions()
    {
        return new List<string>
        {
            "Chào cậu, tớ là một người thích sự đơn giản và chân thành. Nếu cậu cũng đang tìm kiếm một cuộc trò chuyện thú vị sau giờ làm, thì thả tim cho tớ nhé!",
            "Người ta nói giọng nói có thể phản ánh tâm hồn. Cậu nghe giọng tớ xong thấy tâm hồn tớ có hợp với cậu không? Quẹt phải để tớ kể thêm nhé!",
            "Tớ không giỏi thả thính, chỉ biết dùng sự chân thành này để nói rằng tớ rất mong được làm quen với cậu. Match với tớ để mình cùng tìm hiểu nha!"
        };
    }
}
