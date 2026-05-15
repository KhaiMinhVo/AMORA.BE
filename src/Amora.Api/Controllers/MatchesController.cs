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

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<InboxItemDto>>>> GetInbox(
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var result = await _matchService.GetInboxAsync(status, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<InboxItemDto>>.Ok(result));
    }
}