using Amora.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Amora.Api.Controllers;

[ApiController]
[Route("api/upload")]
// [Authorize] // Usually we require authentication to upload, but based on the guide we just implement it.
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
        if (extension != ".m4a" && extension != ".aac" && extension != ".mp3")
            return BadRequest(new { success = false, message = "Định dạng không hỗ trợ" });

        var (uploadUrl, publicUrl) = await _storageService.GeneratePreSignedUploadUrlAsync(extension);

        return Ok(new 
        { 
            success = true, 
            data = new { uploadUrl, publicUrl } 
        });
    }
}
