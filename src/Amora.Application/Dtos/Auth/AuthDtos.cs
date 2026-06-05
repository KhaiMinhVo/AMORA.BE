namespace Amora.Application.Dtos.Auth;

public sealed class RegisterRequest
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Otp { get; set; } = string.Empty;

    public string? DisplayName { get; set; }
}

public sealed class LoginRequest
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

public sealed class AuthResponseDto
{
    public string AccessToken { get; init; } = string.Empty;

    public string TokenType { get; init; } = "Bearer";

    public DateTime ExpiresAt { get; init; }

    public Guid UserId { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public int Diamonds { get; init; }

    public bool RequiresPasswordUpdate { get; init; }
}

public sealed class SubmitAppealRequest
{
    public string Email { get; set; } = string.Empty;
    public string AppealReason { get; set; } = string.Empty;
}

public sealed class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}
