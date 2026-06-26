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

    /// <summary>
    /// Lay feed voice post, ho tro phan trang.
    /// </summary>
    [HttpGet("feed")]
    public async Task<ActionResult<ApiResponse<FeedResponseDto>>> GetFeed(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _voicePostService.GetFeedAsync(page, pageSize, cancellationToken);
        return Ok(ApiResponse<FeedResponseDto>.Ok(result));
    }

    /// <summary>
    /// Lay danh sach voice post cua chinh minh.
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<FeedResponseDto>>> GetMyPosts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _voicePostService.GetMyPostsAsync(page, pageSize, cancellationToken);
        return Ok(ApiResponse<FeedResponseDto>.Ok(result));
    }

    /// <summary>
    /// Tao voice post moi cho user hien tai.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CreateVoicePostResponseDto>>> CreatePost(
        [FromBody] CreateVoicePostRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _voicePostService.CreateAsync(request, cancellationToken);
        return Created($"/api/posts/{result.PostId}", ApiResponse<CreateVoicePostResponseDto>.Ok(result, "Voice post created successfully."));
    }

    /// <summary>
    /// Dong voice post theo postId.
    /// </summary>
    [HttpDelete("{postId:guid}")]
    public async Task<IActionResult> ClosePost(Guid postId, CancellationToken cancellationToken)
    {
        await _voicePostService.CloseAsync(postId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Voice post closed successfully."));
    }

    /// <summary>
    /// Tha cam xuc vao voice post (Like, Love, Haha, Wow, Sad, Angry).
    /// </summary>
    [HttpPost("{postId:guid}/react")]
    public async Task<ActionResult<ApiResponse<ReactToPostResponse>>> ReactToPost(
        Guid postId,
        [FromBody] ReactToPostRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _voicePostService.ReactToPostAsync(postId, request, cancellationToken);
        return Ok(ApiResponse<ReactToPostResponse>.Ok(result, "Reaction updated."));
    }
}