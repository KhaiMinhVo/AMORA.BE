using Amora.Application.Abstractions;
using Amora.Application.Dtos.Auth;
using Amora.Application.Exceptions;
using Amora.Domain.Entities;
using Amora.Application.Iap;
using Amora.Domain.Interfaces;

namespace Amora.Application.Services;

public sealed class AuthService
{
    private readonly IUserRepository _users;
    private readonly IJwtTokenService _jwt;
    private readonly PetCoinRewardService _petCoins;

    public AuthService(IUserRepository users, IJwtTokenService jwt, PetCoinRewardService petCoins)
    {
        _users = users;
        _jwt = jwt;
        _petCoins = petCoins;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            throw new ValidationApiException("Email and password are required.");

        if (await _users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken) is not null)
            throw new ConflictApiException("Email already registered.");

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = PasswordHasher.Hash(request.Password),
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? "Amora User" : request.DisplayName.Trim(),
            AvatarUrl = "default_avatar.png",
            PetCoins = 100,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _users.AddAsync(user, cancellationToken);
        return BuildResponse(user);
    }

    public async Task<AuthResponseDto> DevTokenAsync(Guid userId, string displayName, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdForUpdateAsync(userId, cancellationToken);
        if (user is null)
        {
            user = new AppUser
            {
                Id = userId,
                DisplayName = displayName,
                AvatarUrl = "default_avatar.png",
                PetCoins = 100,
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _users.AddAsync(user, cancellationToken);
        }

        return BuildResponse(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _users.GetByEmailForAuthAsync(email, cancellationToken)
            ?? throw new ValidationApiException("Invalid email or password.");

        if (!PasswordHasher.Verify(request.Password, user.PasswordHash))
            throw new ValidationApiException("Invalid email or password.");

        await _petCoins.TryGrantDailyLoginBonusAsync(user, cancellationToken);
        return BuildResponse(user);
    }

    private AuthResponseDto BuildResponse(AppUser user)
    {
        var token = _jwt.CreateToken(user);
        return new AuthResponseDto
        {
            AccessToken = token.AccessToken,
            TokenType = "Bearer",
            ExpiresAt = token.ExpiresAt,
            UserId = user.Id,
            DisplayName = user.DisplayName,
            PetCoins = user.PetCoins,
            AmoraGems = user.AmoraGems
        };
    }
}
