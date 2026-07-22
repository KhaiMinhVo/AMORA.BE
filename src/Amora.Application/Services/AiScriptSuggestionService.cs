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

        string prompt = "Bạn là trợ lý gợi ý nội dung cho ứng dụng AMORA, một nền tảng kết nối bằng giọng nói.\n" +
                        "Hãy tạo 1 đoạn gợi ý để người dùng đọc khi thu âm \"Giọng nói giới thiệu bản thân\".\n\n" +
                        $"Tên người dùng: {displayName}\n";
        
        if (!string.IsNullOrWhiteSpace(bio))
        {
            prompt += $"Tiểu sử (Bio): {bio}\n";
        }
        if (!string.IsNullOrWhiteSpace(interests))
        {
            prompt += $"Sở thích: {interests}\n";
        }
        
        prompt += "\nMục tiêu:\n" +
                  "- Đoạn gợi ý phải tự nhiên, chân thành, dễ đọc thành tiếng.\n" +
                  "- Khi đọc, nội dung nên kéo dài khoảng 10 đến 30 giây.\n" +
                  "- Nội dung phù hợp với app kết nối/hẹn hò bằng giọng nói.\n" +
                  "- Giúp người nghe cảm nhận được tính cách, sở thích và mong muốn kết nối của người nói.\n" +
                  "- Giọng văn ấm áp, lịch sự, gần gũi, không quá sến, không quá trang trọng.\n\n" +
                  "Yêu cầu:\n" +
                  "1. Chỉ tạo 1 đoạn content duy nhất cho mỗi lần gọi.\n" +
                  "2. Mỗi lần gọi phải tạo một đoạn khác với các lần trước, không lặp lại nguyên văn.\n" +
                  "3. Đoạn gợi ý dài khoảng 2 đến 4 câu ngắn.\n" +
                  "4. Không cần title.\n" +
                  "5. Không giải thích.\n" +
                  "6. Không dùng từ nhạy cảm, phản cảm, tiêu cực hoặc quá riêng tư.\n" +
                  "7. Không nhắc đến thông tin cá nhân cụ thể như số điện thoại, địa chỉ, mạng xã hội.\n" +
                  "8. Nội dung nên thay đổi đa dạng theo nhiều phong cách (nhẹ nhàng, vui vẻ, trưởng thành, hướng nội, năng động, chân thành, hài hước nhẹ, lãng mạn vừa phải).\n" +
                  "9. Không dùng các câu quá chung chung như \"Mình là một người bình thường\".\n" +
                  "10. Không hứa hẹn quá đà hoặc tạo cảm giác giả tạo.\n\n" +
                  "Format trả về:\n" +
                  "{\n" +
                  "  \"content\": \"Xin chào, mình là người thích những cuộc trò chuyện tự nhiên và chân thành...\"\n" +
                  "}\n\n" +
                  "Chỉ trả về JSON object đúng format trên, không giải thích thêm.";

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
                temperature = 0.9,
                topP = 0.9,
                topK = 40,
                maxOutputTokens = 1024,
                responseMimeType = "application/json"
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync($"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}", content, cancellationToken);
            
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
                        try
                        {
                            using var suggestionDoc = JsonDocument.Parse(text);
                            if (suggestionDoc.RootElement.TryGetProperty("content", out var contentProp))
                            {
                                var suggestion = contentProp.GetString();
                                if (!string.IsNullOrWhiteSpace(suggestion))
                                {
                                    return new List<string> { suggestion.Trim() };
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to parse AI suggestion JSON. Raw output: {Text}", text);
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
