using Amora.Domain.Entities;

namespace Amora.Application.Abstractions;

public sealed class AuthTokenResult
{
    public string AccessToken { get; init; } = string.Empty;

    public DateTime ExpiresAt { get; init; }
}

public interface IJwtTokenService
{
    AuthTokenResult CreateToken(AppUser user);
}
