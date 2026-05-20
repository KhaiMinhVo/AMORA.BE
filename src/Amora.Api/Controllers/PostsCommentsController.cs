using Amora.Application.Common;
using Amora.Application.Dtos.Comments;
using Amora.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Amora.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/posts/{postId:guid}/comments")]
public sealed class PostsCommentsController : ControllerBase
{
    private readonly VoiceCommentService _voiceCommentService;

    public PostsCommentsController(VoiceCommentService voiceCommentService)
    {
        _voiceCommentService = voiceCommentService;
    }

    /// <summary>
    /// Tao voice comment cho post, ap dung rate limit.
    /// </summary>
    [HttpPost]
    [EnableRateLimiting("comment")]
    public async Task<ActionResult<ApiResponse<CreateCommentResponseDto>>> CreateComment(
        Guid postId,
        [FromBody] CreateVoiceCommentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _voiceCommentService.CreateCommentAsync(postId, request, cancellationToken);
        return Created($"/api/posts/{postId}/comments/{result.CommentId}", ApiResponse<CreateCommentResponseDto>.Ok(result, "Voice comment created successfully."));
    }

    /// <summary>
    /// Lay danh sach comment cua post, ho tro phan trang.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<VoiceCommentListResponseDto>>> GetComments(
        Guid postId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _voiceCommentService.GetCommentsAsync(postId, page, pageSize, cancellationToken);
        return Ok(ApiResponse<VoiceCommentListResponseDto>.Ok(result));
    }
}