using Amora.Application.Common;
using Amora.Application.Dtos.Messages;
using Amora.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Amora.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/matches/{matchId:guid}/messages")]
public sealed class ChatController : ControllerBase
{
    private readonly ChatService _chatService;

    public ChatController(ChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<MessageHistoryResponseDto>>> GetMessages(
        Guid matchId,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _chatService.GetHistoryAsync(matchId, cursor, limit, cancellationToken);
        return Ok(ApiResponse<MessageHistoryResponseDto>.Ok(result));
    }

    [HttpPost]
    [EnableRateLimiting("message")]
    public async Task<ActionResult<ApiResponse<SendMessageResponseDto>>> SendMessage(
        Guid matchId,
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _chatService.SendMessageAsync(matchId, request, cancellationToken);
        return Ok(ApiResponse<SendMessageResponseDto>.Ok(result, "Message sent successfully."));
    }
}