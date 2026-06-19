using Amora.Application.Dtos.Support;
using Amora.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Amora.Api.Controllers;

[ApiController]
[Route("api/users/me/support-tickets")]
[Authorize]
public class UserSupportController : ControllerBase
{
    private readonly SupportTicketService _supportTicketService;

    public UserSupportController(SupportTicketService supportTicketService)
    {
        _supportTicketService = supportTicketService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequest request, CancellationToken cancellationToken)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdString, out var userId))
            return Unauthorized();

        await _supportTicketService.CreateTicketAsync(userId, request, cancellationToken);
        return Ok(new { message = "Gửi khiếu nại thành công." });
    }
}
