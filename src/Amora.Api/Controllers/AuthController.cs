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

    /// <summary>
    /// Dang ky tai khoan moi va tra ve token.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(request, cancellationToken);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Registration successful."));
    }

    /// <summary>
    /// Dang nhap va tra ve token cho phien.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Login successful."));
    }

    /// <summary>
    /// Dang nhap bang Google IdToken.
    /// </summary>
    [HttpPost("google")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> GoogleLogin(
        [FromBody] LoginWithGoogleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LoginWithGoogleAsync(request, cancellationToken);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Google login successful."));
    }

    /// <summary>
    /// Gui ma OTP den Email.
    /// </summary>
    [HttpPost("email/send-otp")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<string>>> SendEmailOtp(
        [FromBody] SendEmailOtpRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.SendEmailOtpAsync(request, cancellationToken);
        return Ok(ApiResponse<string>.Ok("OTP sent.", "OTP sent successfully to email."));
    }

    /// <summary>
    /// Xac minh OTP va dang nhap bang Email.
    /// </summary>
    [HttpPost("email/verify")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> EmailOtpLogin(
        [FromBody] LoginWithEmailOtpRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LoginWithEmailOtpAsync(request, cancellationToken);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Email OTP login successful."));
    }

    /// <summary>
    /// Thiet lap mat khau cho tai khoan moi (tu Google/Phone).
    /// </summary>
    [HttpPost("set-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<string>>> SetPassword(
        [FromBody] SetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var userIdString = User.FindFirst("id")?.Value;
        if (!Guid.TryParse(userIdString, out var userId))
            return Unauthorized();

        await _authService.SetPasswordAsync(userId, request, cancellationToken);
        return Ok(ApiResponse<string>.Ok("Password set.", "Password updated successfully."));
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
