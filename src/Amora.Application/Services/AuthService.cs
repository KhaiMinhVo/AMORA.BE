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
    private readonly IEmailService _emailService;
    private readonly IMemoryCache _memoryCache;
    private readonly IConfiguration _configuration;
    private readonly IUserBanRepository _userBanRepository;
    private readonly AdminNotificationService _adminNotificationService;

    public AuthService(
        IUserRepository users, 
        IJwtTokenService jwt, 
        DiamondRewardService petCoins,
        IEmailService emailService,
        IMemoryCache memoryCache,
        IConfiguration configuration,
        IUserBanRepository userBanRepository,
        AdminNotificationService adminNotificationService)
    {
        _users = users;
        _jwt = jwt;
        _petCoins = petCoins;
        _emailService = emailService;
        _memoryCache = memoryCache;
        _configuration = configuration;
        _userBanRepository = userBanRepository;
        _adminNotificationService = adminNotificationService;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            throw new ValidationApiException("Email và mật khẩu là bắt buộc.");

        var email = request.Email.Trim().ToLowerInvariant();
        var cacheKey = $"OTP_{email}";
        if (!_memoryCache.TryGetValue(cacheKey, out string? cachedOtp) || cachedOtp != request.Otp)
            throw new ValidationApiException("Mã OTP không đúng hoặc đã hết hạn.");
        
        _memoryCache.Remove(cacheKey);

        if (await _users.GetByEmailAsync(email, cancellationToken) is not null)
            throw new ConflictApiException("Email này đã được đăng ký.");

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = email,
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
            ?? throw new ValidationApiException("Email hoặc mật khẩu không đúng.");

        if (string.IsNullOrEmpty(user.PasswordHash) || !PasswordHasher.Verify(request.Password, user.PasswordHash))
            throw new ValidationApiException("Email hoặc mật khẩu không đúng.");

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
            throw new ValidationApiException("Token Google không hợp lệ.");
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

    public async Task SendRegisterOtpAsync(SendEmailOtpRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _users.GetByEmailAsync(email, cancellationToken) is not null)
            throw new ConflictApiException("Email này đã được đăng ký.");

        await SendOtpInternalAsync(email, "AMORA - Mã xác nhận đăng ký", cancellationToken);
    }

    public async Task SendForgotPasswordOtpAsync(SendEmailOtpRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _users.GetByEmailAsync(email, cancellationToken) is null)
            throw new NotFoundApiException("Không tìm thấy tài khoản.");

        await SendOtpInternalAsync(email, "AMORA - Mã xác nhận đặt lại mật khẩu", cancellationToken);
    }

    private async Task SendOtpInternalAsync(string email, string subject, CancellationToken cancellationToken)
    {
        var otp = new Random().Next(100000, 999999).ToString();
        var body = $"<h3>Mã xác nhận của bạn là: <strong>{otp}</strong></h3><p>Mã có hiệu lực trong 5 phút. Vui lòng không chia sẻ mã này cho bất kỳ ai.</p>";

        var success = await _emailService.SendEmailAsync(email, subject, body, cancellationToken);
        if (!success)
            throw new ValidationApiException("Không thể gửi mã OTP đến email này.");

        var cacheKey = $"OTP_{email}";
        _memoryCache.Set(cacheKey, otp, TimeSpan.FromMinutes(5));
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var cacheKey = $"OTP_{email}";
        if (!_memoryCache.TryGetValue(cacheKey, out string? cachedOtp) || cachedOtp != request.Otp)
            throw new ValidationApiException("Mã OTP không đúng hoặc đã hết hạn.");

        _memoryCache.Remove(cacheKey);

        var user = await _users.GetByEmailForAuthAsync(email, cancellationToken)
            ?? throw new NotFoundApiException("Không tìm thấy tài khoản.");

        user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
        user.RequiresPasswordUpdate = false;
        await _users.UpdateAsync(user, cancellationToken);
    }

    public async Task SetPasswordAsync(Guid userId, SetPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdForUpdateAsync(userId, cancellationToken)
            ?? throw new NotFoundApiException("Không tìm thấy người dùng.");

        user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
        user.RequiresPasswordUpdate = false;
        await _users.UpdateAsync(user, cancellationToken);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdForUpdateAsync(userId, cancellationToken)
            ?? throw new NotFoundApiException("Không tìm thấy người dùng.");

        if (string.IsNullOrEmpty(user.PasswordHash) || !PasswordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new ValidationApiException("Mật khẩu hiện tại không đúng.");
        }

        user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
        await _users.UpdateAsync(user, cancellationToken);
    }

    public async Task SubmitAppealAsync(SubmitAppealRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _users.GetByEmailForAuthAsync(email, cancellationToken)
            ?? throw new NotFoundApiException("Không tìm thấy tài khoản.");

        if (!user.IsBanned)
        {
            throw new ValidationApiException("Tài khoản này không bị khóa.");
        }

        var activeBan = await _userBanRepository.GetActiveBanByUserIdAsync(user.Id, cancellationToken);
        if (activeBan == null)
        {
            throw new ValidationApiException("Bạn không có lệnh cấm nào để kháng cáo.");
        }

        if (activeBan.AppealStatus == Amora.Domain.Enums.AppealStatus.Pending)
        {
            throw new ConflictApiException("Bạn đã có một đơn kháng cáo đang chờ xử lý.");
        }

        if (string.IsNullOrWhiteSpace(request.AppealReason))
        {
            throw new ValidationApiException("Vui lòng nhập lý do kháng cáo.");
        }

        activeBan.AppealStatus = Amora.Domain.Enums.AppealStatus.Pending;
        activeBan.AppealReason = request.AppealReason.Trim();
        await _userBanRepository.UpdateAsync(activeBan, cancellationToken);
        await _adminNotificationService.NotifyNewAppealAsync(user.Id, user.DisplayName, activeBan.AppealReason, cancellationToken);
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
