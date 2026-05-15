using Amora.Application.Common;
using Amora.Application.Dtos.Posts;
using Amora.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Amora.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/posts")]
public sealed class PostsController : ControllerBase
{
    private readonly VoicePostService _voicePostService;

    public PostsController(VoicePostService voicePostService)
    {
        _voicePostService = voicePostService;
    }

    [HttpGet("feed")]
    public async Task<ActionResult<ApiResponse<FeedResponseDto>>> GetFeed(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _voicePostService.GetFeedAsync(page, pageSize, cancellationToken);
        return Ok(ApiResponse<FeedResponseDto>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CreateVoicePostResponseDto>>> CreatePost(
        [FromBody] CreateVoicePostRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _voicePostService.CreateAsync(request, cancellationToken);
        return Created($"/api/posts/{result.PostId}", ApiResponse<CreateVoicePostResponseDto>.Ok(result, "Voice post created successfully."));
    }

    [HttpDelete("{postId:guid}")]
    public async Task<IActionResult> ClosePost(Guid postId, CancellationToken cancellationToken)
    {
        await _voicePostService.CloseAsync(postId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Voice post closed successfully."));
    }
}