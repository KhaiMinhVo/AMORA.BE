using Amora.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Amora.Api.Controllers;

[ApiController]
[Route("api/upload")]
[Authorize]
public class UploadController : ControllerBase
{
    private readonly IStorageService _storageService;

    public UploadController(IStorageService storageService)
    {
        _storageService = storageService;
    }

    [HttpGet("presigned-url")]
    public async Task<IActionResult> GetPresignedUrl([FromQuery] string extension = ".m4a")
    {
        // Validate định dạng cho phép
        extension = extension.ToLowerInvariant();
        if (extension != ".m4a" && extension != ".aac" && extension != ".mp3" && extension != ".webm")
            return BadRequest(new { success = false, message = "Định dạng không hỗ trợ" });

        var (uploadUrl, publicUrl) = await _storageService.GeneratePreSignedUploadUrlAsync(extension);

        return Ok(new 
        { 
            success = true, 
            data = new { uploadUrl, publicUrl } 
        });
    }

    [HttpGet("presigned-image-url")]
    public async Task<IActionResult> GetPresignedImageUrl([FromQuery] string extension = ".jpg")
    {
        extension = extension.ToLowerInvariant();
        if (extension is not ".jpg" and not ".jpeg" and not ".png" and not ".webp")
            return BadRequest(new { success = false, message = "Định dạng ảnh không hỗ trợ" });

        var (uploadUrl, publicUrl) = await _storageService.GeneratePreSignedUploadUrlAsync(extension, "chat-images");

        return Ok(new
        {
            success = true,
            data = new { uploadUrl, publicUrl }
        });
    }

    /// <summary>
    /// Upload ảnh trực tiếp qua Backend (không cần presigned URL).
    /// Gửi file qua form-data với key "file".
    /// </summary>
    [HttpPost("image")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB max
    [DisableRequestSizeLimit]
    public async Task<IActionResult> UploadImage()
    {
        var file = Request.Form.Files.FirstOrDefault();
        if (file == null || file.Length == 0)
            return BadRequest(new { success = false, message = "Không có file nào được gửi lên." });

        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? ".jpg";
        if (extension is not ".jpg" and not ".jpeg" and not ".png" and not ".webp")
            return BadRequest(new { success = false, message = "Định dạng ảnh không hỗ trợ." });

        using var stream = file.OpenReadStream();

        string mimeType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };

        try
        {
            var publicUrl = await _storageService.UploadFileAsync(stream, extension, "chat-images", mimeType);

            return Ok(new
            {
                success = true,
                data = new { publicUrl }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new 
            { 
                success = false, 
                message = "Upload failed", 
                error = ex.Message,
                stackTrace = ex.StackTrace 
            });
        }
    }

    /// <summary>
    /// Upload file âm thanh (voice) trực tiếp qua Backend.
    /// Gửi file qua form-data với field name bất kỳ.
    /// </summary>
    [HttpPost("voice")]
    [RequestSizeLimit(20 * 1024 * 1024)] // 20MB max
    [DisableRequestSizeLimit]
    public async Task<IActionResult> UploadVoice()
    {
        var file = Request.Form.Files.FirstOrDefault();
        if (file == null || file.Length == 0)
            return BadRequest(new { success = false, message = "Không có file nào được gửi lên." });

        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? ".m4a";
        if (extension is not ".m4a" and not ".aac" and not ".mp3" and not ".wav" and not ".webm")
            return BadRequest(new { success = false, message = "Định dạng âm thanh không hỗ trợ." });

        using var stream = file.OpenReadStream();

        string mimeType = extension switch
        {
            ".m4a" => "audio/mp4",
            ".aac" => "audio/aac",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".webm" => "audio/webm",
            _ => "application/octet-stream"
        };

        try
        {
            var publicUrl = await _storageService.UploadFileAsync(stream, extension, "voices", mimeType);

            return Ok(new
            {
                success = true,
                data = new { publicUrl }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "Upload failed",
                error = ex.Message,
                stackTrace = ex.StackTrace
            });
        }
    }
}
