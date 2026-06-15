using Amora.Application.Abstractions;
using Amora.Application.Dtos.Profile;
using Amora.Application.Exceptions;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;

namespace Amora.Application.Services;

public sealed class ProfileService
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserRepository _userRepository;
    private readonly IMatchConnectionRepository _matchConnectionRepository;

    private const string DefaultAnonymousAvatar = "anonymous.png";

    public ProfileService(
        ICurrentUserService currentUserService,
        IUserRepository userRepository,
        IMatchConnectionRepository matchConnectionRepository)
    {
        _currentUserService = currentUserService;
        _userRepository = userRepository;
        _matchConnectionRepository = matchConnectionRepository;
    }

    /// <summary>Lấy profile của chính mình (đầy đủ thông tin).</summary>
    public async Task<ProfileResponseDto> GetMyProfileAsync(CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(_currentUserService.UserId, cancellationToken)
            ?? throw new NotFoundApiException("User not found.");

        return MapToProfileResponse(user);
    }

    /// <summary>
    /// Lấy profile công khai của user khác.
    /// Avatar và Photos bị ẩn nếu chưa match với viewer.
    /// </summary>
    public async Task<PublicProfileResponseDto> GetPublicProfileAsync(Guid targetUserId, CancellationToken cancellationToken = default)
    {
        var target = await _userRepository.GetByIdAsync(targetUserId, cancellationToken)
            ?? throw new NotFoundApiException("User not found.");

        var viewerId = _currentUserService.UserId;

        // Kiểm tra đã match chưa để quyết định hiển thị Avatar và Photos
        var isMatched = await _matchConnectionRepository.AreMatchedAsync(viewerId, targetUserId, cancellationToken);

        return new PublicProfileResponseDto
        {
            UserId = target.Id,
            DisplayName = target.DisplayName,
            AvatarUrl = isMatched ? target.AvatarUrl : DefaultAnonymousAvatar,
            Photos = isMatched ? target.Photos : Array.Empty<string>(),
            Gender = target.Gender.ToString(),
            City = target.City,
            Bio = target.Bio,
            VoiceIntroUrl = target.VoiceIntroUrl,
            Interests = ParseInterests(target.Interests)
        };
    }

    /// <summary>Cập nhật profile của chính mình.</summary>
    public async Task<ProfileResponseDto> UpdateMyProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(_currentUserService.UserId, cancellationToken)
            ?? throw new NotFoundApiException("User not found.");

        if (!string.IsNullOrWhiteSpace(request.DisplayName))
            user.DisplayName = request.DisplayName.Trim();

        if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
            user.AvatarUrl = request.AvatarUrl.Trim();

        if (request.Photos is not null)
            user.Photos = request.Photos.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();

        if (!string.IsNullOrWhiteSpace(request.DateOfBirth))
        {
            if (!DateOnly.TryParse(request.DateOfBirth, out var dob))
                throw new ValidationApiException("DateOfBirth must be in yyyy-MM-dd format.");

            // Validate tuổi >= 18
            var age = DateOnly.FromDateTime(DateTime.UtcNow).Year - dob.Year;
            if (age < 18)
                throw new ValidationApiException("You must be at least 18 years old.");

            user.DateOfBirth = dob;
        }

        if (!string.IsNullOrWhiteSpace(request.Gender))
        {
            if (!Enum.TryParse<Gender>(request.Gender, ignoreCase: true, out var gender))
                throw new ValidationApiException($"Invalid gender. Valid values: {string.Join(", ", Enum.GetNames<Gender>())}");
            user.Gender = gender;
        }

        if (!string.IsNullOrWhiteSpace(request.TargetGender))
        {
            if (!Enum.TryParse<TargetGender>(request.TargetGender, ignoreCase: true, out var targetGender))
                throw new ValidationApiException($"Invalid target gender. Valid values: {string.Join(", ", Enum.GetNames<TargetGender>())}");
            user.TargetGender = targetGender;
        }

        if (request.City is not null)
            user.City = request.City.Trim();

        if (request.Bio is not null)
        {
            if (request.Bio.Length > 300)
                throw new ValidationApiException("Bio must not exceed 300 characters.");
            user.Bio = request.Bio.Trim();
        }

        if (request.Interests is not null)
            user.Interests = string.Join(",", request.Interests.Select(i => i.Trim()).Where(i => i.Length > 0));

        if (request.VoiceIntroUrl is not null)
            user.VoiceIntroUrl = request.VoiceIntroUrl.Trim();

        // Kiểm tra đã đủ thông tin chưa: có avatar, có DOB, có giới tính, và có ít nhất 2 ảnh.
        user.IsProfileComplete = !string.IsNullOrWhiteSpace(user.DisplayName)
                                  && !string.IsNullOrWhiteSpace(user.AvatarUrl)
                                  && user.DateOfBirth.HasValue
                                  && user.Gender != Gender.PreferNotToSay
                                  && user.Photos is not null && user.Photos.Length >= 2;

        await _userRepository.UpdateAsync(user, cancellationToken);

        return MapToProfileResponse(user);
    }

    private static ProfileResponseDto MapToProfileResponse(Domain.Entities.AppUser user) => new()
    {
        UserId = user.Id,
        DisplayName = user.DisplayName,
        AvatarUrl = user.AvatarUrl,
        Photos = user.Photos,
        DateOfBirth = user.DateOfBirth?.ToString("yyyy-MM-dd"),
        Gender = user.Gender.ToString(),
        TargetGender = user.TargetGender.ToString(),
        City = user.City,
        Bio = user.Bio,
        VoiceIntroUrl = user.VoiceIntroUrl,
        Interests = ParseInterests(user.Interests),
        IsProfileComplete = user.IsProfileComplete,
        CreatedAt = user.CreatedAt,
        Diamonds = user.Diamonds,
        SubscriptionType = user.SubscriptionType.ToString(),
        SubscriptionEndDate = user.SubscriptionEndDate
    };

    private static string[] ParseInterests(string? interests)
    {
        if (string.IsNullOrWhiteSpace(interests)) return [];
        return interests.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
