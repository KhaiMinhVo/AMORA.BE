using Amora.Application.Common;
using Amora.Application.Dtos.Matches;
using Amora.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Amora.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/matches")]
public sealed class MatchesController : ControllerBase
{
    private readonly MatchService _matchService;

    public MatchesController(MatchService matchService)
    {
        _matchService = matchService;
    }

    /// <summary>
    /// Tao match moi tu yeu cau tuong tac.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<MatchCreatedResponseDto>>> CreateMatch(
        [FromBody] CreateMatchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _matchService.CreateMatchAsync(request, cancellationToken);
        var message = result.PostClosed
            ? "Match created successfully. Post is now Closed."
            : "Match created successfully.";

        return Created($"/api/matches/{result.MatchId}", ApiResponse<MatchCreatedResponseDto>.Ok(result, message));
    }

    /// <summary>
    /// Huy ket noi match va dong phien.
    /// </summary>
    [HttpPost("{matchId:guid}/unmatch")]
    public async Task<ActionResult<ApiResponse<object>>> Unmatch(Guid matchId, CancellationToken cancellationToken)
    {
        await _matchService.UnmatchAsync(matchId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Match unmatched successfully."));
    }

    /// <summary>
    /// Chap nhan yeu cau ket noi (Danh cho UserB).
    /// </summary>
    [HttpPost("{matchId:guid}/accept")]
    public async Task<ActionResult<ApiResponse<object>>> AcceptMatch(Guid matchId, CancellationToken cancellationToken)
    {
        await _matchService.AcceptMatchAsync(matchId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Match request accepted successfully."));
    }

    /// <summary>
    /// Tu choi yeu cau ket noi (Danh cho UserB).
    /// </summary>
    [HttpPost("{matchId:guid}/reject")]
    public async Task<ActionResult<ApiResponse<object>>> RejectMatch(Guid matchId, CancellationToken cancellationToken)
    {
        await _matchService.RejectMatchAsync(matchId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Match request rejected successfully."));
    }

    /// <summary>
    /// Làm lại yêu cầu kết nối đã từ chối (Chỉ dành cho Premium/Gold).
    /// </summary>
    [HttpPost("{matchId:guid}/rematch")]
    public async Task<ActionResult<ApiResponse<object>>> RematchMatch(Guid matchId, CancellationToken cancellationToken)
    {
        await _matchService.RematchAsync(matchId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Match rematch accepted successfully."));
    }

    /// <summary>
    /// Lấy thông tin quota match của user (số lượt còn lại trong ngày).
    /// </summary>
    [HttpGet("quota")]
    public async Task<ActionResult<ApiResponse<MatchQuotaDto>>> GetQuota(CancellationToken cancellationToken)
    {
        var userIdString = User.FindFirst("id")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdString, out var userId))
            return Unauthorized(ApiResponse<object>.Fail("Invalid user token.", "UNAUTHORIZED"));

        var result = await _matchService.GetQuotaAsync(userId, cancellationToken);
        return Ok(ApiResponse<MatchQuotaDto>.Ok(result));
    }

    /// <summary>
    /// Lay inbox match theo trang thai (neu co).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<InboxItemDto>>>> GetInbox(
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var result = await _matchService.GetInboxAsync(status, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<InboxItemDto>>.Ok(result));
    }
}