using Amora.Application.Abstractions;
using Amora.Application.Dtos.Auth;
using Amora.Application.Exceptions;
using Amora.Domain.Entities;
using Amora.Application.Iap;
using Amora.Domain.Interfaces;
using Google.Apis.Auth;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace Amora.Application.Services;

public sealed class AuthService
{
    private readonly IUserRepository _users;
    private readonly IJwtTokenService _jwt;
    private readonly DiamondRewardService _petCoins;
    private readonly ISmsService _smsService;
    private readonly IMemoryCache _memoryCache;
    private readonly IConfiguration _configuration;

    public AuthService(
        IUserRepository users, 
        IJwtTokenService jwt, 
        DiamondRewardService petCoins,
        ISmsService smsService,
        IMemoryCache memoryCache,
        IConfiguration configuration)
    {
        _users = users;
        _jwt = jwt;
        _petCoins = petCoins;
        _smsService = smsService;
        _memoryCache = memoryCache;
        _configuration = configuration;
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
            Diamonds = 0,
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
                Diamonds = 0,
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

        if (string.IsNullOrEmpty(user.PasswordHash) || !PasswordHasher.Verify(request.Password, user.PasswordHash))
            throw new ValidationApiException("Invalid email or password.");

        await _petCoins.TryGrantDailyLoginBonusAsync(user, cancellationToken);
        return BuildResponse(user);
    }

    public async Task<AuthResponseDto> LoginWithGoogleAsync(LoginWithGoogleRequest request, CancellationToken cancellationToken)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            var clientId = _configuration["Google:ClientId"];
            var settings = new GoogleJsonWebSignature.ValidationSettings();
            if (!string.IsNullOrWhiteSpace(clientId))
            {
                settings.Audience = new[] { clientId };
            }
            
            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
        }
        catch (InvalidJwtException)
        {
            throw new ValidationApiException("Invalid Google token.");
        }

        var user = await _users.GetByGoogleIdAsync(payload.Subject, cancellationToken);
        if (user is null && !string.IsNullOrWhiteSpace(payload.Email))
        {
            user = await _users.GetByEmailForAuthAsync(payload.Email, cancellationToken);
        }

        if (user is null)
        {
            // Register new user
            user = new AppUser
            {
                Id = Guid.NewGuid(),
                Email = payload.Email,
                GoogleId = payload.Subject,
                DisplayName = string.IsNullOrWhiteSpace(payload.Name) ? "Google User" : payload.Name,
                AvatarUrl = string.IsNullOrWhiteSpace(payload.Picture) ? "default_avatar.png" : payload.Picture,
                Diamonds = 0,
                CreatedAt = DateTimeOffset.UtcNow,
                RequiresPasswordUpdate = true
            };
            await _users.AddAsync(user, cancellationToken);
        }
        else
        {
            // Update GoogleId if it was not linked
            if (string.IsNullOrWhiteSpace(user.GoogleId))
            {
                user.GoogleId = payload.Subject;
                await _users.UpdateAsync(user, cancellationToken);
            }
        }

        await _petCoins.TryGrantDailyLoginBonusAsync(user, cancellationToken);
        return BuildResponse(user);
    }

    public async Task SendOtpAsync(SendOtpRequest request, CancellationToken cancellationToken)
    {
        var otp = new Random().Next(100000, 999999).ToString();
        var message = $"[Amora] Ma xac nhan cua ban la: {otp}. Ma co hieu luc trong 5 phut.";

        var success = await _smsService.SendSmsAsync(request.PhoneNumber, message, cancellationToken);
        if (!success)
        {
            throw new ValidationApiException("Failed to send OTP to the provided phone number.");
        }

        var cacheKey = $"OTP_{request.PhoneNumber}";
        _memoryCache.Set(cacheKey, otp, TimeSpan.FromMinutes(5));
    }

    public async Task<AuthResponseDto> LoginWithPhoneAsync(LoginWithPhoneRequest request, CancellationToken cancellationToken)
    {
        var cacheKey = $"OTP_{request.PhoneNumber}";
        if (!_memoryCache.TryGetValue(cacheKey, out string? cachedOtp) || cachedOtp != request.Otp)
        {
            throw new ValidationApiException("Invalid or expired OTP.");
        }

        _memoryCache.Remove(cacheKey);

        var user = await _users.GetByPhoneNumberAsync(request.PhoneNumber, cancellationToken);
        if (user is null)
        {
            user = new AppUser
            {
                Id = Guid.NewGuid(),
                PhoneNumber = request.PhoneNumber,
                DisplayName = "Phone User",
                AvatarUrl = "default_avatar.png",
                Diamonds = 0,
                CreatedAt = DateTimeOffset.UtcNow,
                RequiresPasswordUpdate = true
            };
            await _users.AddAsync(user, cancellationToken);
        }

        await _petCoins.TryGrantDailyLoginBonusAsync(user, cancellationToken);
        return BuildResponse(user);
    }

    public async Task SetPasswordAsync(Guid userId, SetPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdForUpdateAsync(userId, cancellationToken)
            ?? throw new NotFoundApiException("User not found.");

        user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
        user.RequiresPasswordUpdate = false;
        await _users.UpdateAsync(user, cancellationToken);
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
            Diamonds = user.Diamonds,
            RequiresPasswordUpdate = user.RequiresPasswordUpdate
        };
    }
}
