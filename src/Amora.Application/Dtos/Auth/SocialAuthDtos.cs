using System.ComponentModel.DataAnnotations;

namespace Amora.Application.Dtos.Auth;

public class LoginWithGoogleRequest
{
    [Required]
    public string IdToken { get; set; } = string.Empty;
}

public class SendEmailOtpRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class LoginWithEmailOtpRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string Otp { get; set; } = string.Empty;
}

public class SetPasswordRequest
{
    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}
