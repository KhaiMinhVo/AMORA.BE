using Amora.Application.Common;
using Amora.Application.Dtos.Auth;
using Amora.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Amora.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService) => _authService = authService;

    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(request, cancellationToken);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Registration successful."));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Login successful."));
    }

    /// <summary>Development — JWT cho seed user hoặc user mới.</summary>
    [HttpPost("dev-token")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> DevToken(
        [FromBody] DevTokenRequest request,
        CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
            return BadRequest(ApiResponse<AuthResponseDto>.Fail("UserId required.", "VALIDATION_ERROR"));

        var name = string.IsNullOrWhiteSpace(request.DisplayName) ? "Dev User" : request.DisplayName;
        var result = await _authService.DevTokenAsync(request.UserId, name, cancellationToken);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result));
    }
}

public sealed class DevTokenRequest
{
    public Guid UserId { get; set; }

    public string DisplayName { get; set; } = "Dev User";
}
