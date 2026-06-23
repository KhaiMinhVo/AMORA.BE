using Amora.Application.Abstractions;
using Amora.Application.Dtos.Profile;
using Amora.Application.Exceptions;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;
using Amora.Application.Iap;

namespace Amora.Application.Services;

public sealed class ProfileService
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserRepository _userRepository;
    private readonly IMatchConnectionRepository _matchConnectionRepository;
    private readonly TrustScoreService _trustScoreService;
    private readonly DiamondRewardService _diamondRewardService;

    private const string DefaultAnonymousAvatar = "anonymous.png";

    public ProfileService(
        ICurrentUserService currentUserService,
        IUserRepository userRepository,
        IMatchConnectionRepository matchConnectionRepository,
        TrustScoreService trustScoreService,
        DiamondRewardService diamondRewardService)
    {
        _currentUserService = currentUserService;
        _userRepository = userRepository;
        _matchConnectionRepository = matchConnectionRepository;
        _trustScoreService = trustScoreService;
        _diamondRewardService = diamondRewardService;
    }

    /// <summary>Lấy profile của chính mình (đầy đủ thông tin).</summary>
    public async Task<ProfileResponseDto> GetMyProfileAsync(CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(_currentUserService.UserId, cancellationToken)
            ?? throw new NotFoundApiException("Không tìm thấy người dùng.");

        return MapToProfileResponse(user);
    }

    /// <summary>
    /// Lấy profile công khai của user khác.
    /// Avatar và Photos bị ẩn nếu chưa match với viewer.
    /// </summary>
    public async Task<PublicProfileResponseDto> GetPublicProfileAsync(Guid targetUserId, CancellationToken cancellationToken = default)
    {
        var target = await _userRepository.GetByIdAsync(targetUserId, cancellationToken)
            ?? throw new NotFoundApiException("Không tìm thấy người dùng.");

        var viewerId = _currentUserService.UserId;

        // Kiểm tra đã match chưa để quyết định hiển thị Avatar và Photos
        var isMatched = await _matchConnectionRepository.AreMatchedAsync(viewerId, targetUserId, cancellationToken);

        // Kiểm tra quyền riêng tư Voice
        var canHearVoice = true;
        if (target.VoicePrivacy == PrivacyLevel.Private) canHearVoice = false;
        if (target.VoicePrivacy == PrivacyLevel.MatchedOnly && !isMatched) canHearVoice = false;

        return new PublicProfileResponseDto
        {
            UserId = target.Id,
            DisplayName = target.DisplayName,
            AvatarUrl = isMatched ? target.AvatarUrl : DefaultAnonymousAvatar,
            Photos = isMatched ? target.Photos : Array.Empty<string>(),
            Gender = target.Gender.ToString(),
            City = target.City,
            Bio = target.Bio,
            VoiceIntroUrl = canHearVoice ? target.VoiceIntroUrl : null,
            VoiceIntroDuration = canHearVoice ? target.VoiceIntroDuration : null,
            Interests = ParseInterests(target.Interests),
            TrustScore = target.TrustScore
        };
    }

    /// <summary>Cập nhật profile của chính mình.</summary>
    public async Task<ProfileResponseDto> UpdateMyProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdForUpdateAsync(_currentUserService.UserId, cancellationToken)
            ?? throw new NotFoundApiException("Không tìm thấy người dùng.");

        if (!string.IsNullOrWhiteSpace(request.DisplayName))
            user.DisplayName = request.DisplayName.Trim();

        if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
            user.AvatarUrl = request.AvatarUrl.Trim();

        if (request.Photos is not null)
            user.Photos = request.Photos.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();

        if (!string.IsNullOrWhiteSpace(request.DateOfBirth))
        {
            if (!DateOnly.TryParse(request.DateOfBirth, out var dob))
                throw new ValidationApiException("Ngày sinh phải theo định dạng yyyy-MM-dd.");

            // Validate tuổi >= 18
            var age = DateOnly.FromDateTime(DateTime.UtcNow).Year - dob.Year;
            if (age < 18)
                throw new ValidationApiException("Bạn phải đủ 18 tuổi trở lên.");

            user.DateOfBirth = dob;
        }

        if (!string.IsNullOrWhiteSpace(request.Gender))
        {
            if (!Enum.TryParse<Gender>(request.Gender, ignoreCase: true, out var gender))
                throw new ValidationApiException($"Giới tính không hợp lệ. Các giá trị cho phép: {string.Join(", ", Enum.GetNames<Gender>())}");
            user.Gender = gender;
        }

        if (!string.IsNullOrWhiteSpace(request.TargetGender))
        {
            if (!Enum.TryParse<TargetGender>(request.TargetGender, ignoreCase: true, out var targetGender))
                throw new ValidationApiException($"Giới tính đối tượng tìm kiếm không hợp lệ. Các giá trị cho phép: {string.Join(", ", Enum.GetNames<TargetGender>())}");
            user.TargetGender = targetGender;
        }

        if (request.City is not null)
            user.City = request.City.Trim();

        if (request.Bio is not null)
        {
            if (request.Bio.Length > 300)
                throw new ValidationApiException("Tiểu sử (Bio) không được vượt quá 300 ký tự.");
            user.Bio = request.Bio.Trim();
        }

        if (request.Interests is not null)
            user.Interests = string.Join(",", request.Interests.Select(i => i.Trim()).Where(i => i.Length > 0));

        if (request.VoiceIntroUrl is not null)
            user.VoiceIntroUrl = string.IsNullOrWhiteSpace(request.VoiceIntroUrl) ? null : request.VoiceIntroUrl.Trim();

        if (request.VoiceIntroDuration.HasValue)
            user.VoiceIntroDuration = request.VoiceIntroDuration.Value;

        if (!string.IsNullOrWhiteSpace(request.VoicePrivacy))
        {
            if (!Enum.TryParse<PrivacyLevel>(request.VoicePrivacy, ignoreCase: true, out var privacy))
                throw new ValidationApiException($"Quyền riêng tư không hợp lệ. Các giá trị cho phép: {string.Join(", ", Enum.GetNames<PrivacyLevel>())}");
            user.VoicePrivacy = privacy;
        }

        // Kiểm tra đã đủ thông tin chưa: có avatar, có DOB, có giới tính.
        user.IsProfileComplete = !string.IsNullOrWhiteSpace(user.DisplayName)
                                  && !string.IsNullOrWhiteSpace(user.AvatarUrl)
                                  && user.DateOfBirth.HasValue
                                  && user.Gender != Gender.PreferNotToSay;

        await _userRepository.UpdateAsync(user, cancellationToken);

        if (user.IsProfileComplete)
        {
            await _trustScoreService.AddProfileCompletionBonusAsync(user.Id, cancellationToken);
        }

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
        VoiceIntroDuration = user.VoiceIntroDuration,
        VoicePrivacy = user.VoicePrivacy.ToString(),
        Interests = ParseInterests(user.Interests),
        IsProfileComplete = user.IsProfileComplete,
        CreatedAt = user.CreatedAt,
        Diamonds = user.Diamonds,
        SubscriptionType = user.SubscriptionType.ToString(),
        SubscriptionEndDate = user.SubscriptionEndDate,
        TrustScore = user.TrustScore,
        AutoRenewEnabled = user.IsAutoRenewEnabled
    };

    private static string[] ParseInterests(string? interests)
    {
        if (string.IsNullOrWhiteSpace(interests)) return [];
        return interests.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public async Task<AttendanceResponseDto> ClaimAttendanceAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        var user = await _userRepository.GetByIdForUpdateAsync(userId, cancellationToken)
            ?? throw new NotFoundApiException("Không tìm thấy người dùng.");

        int diamondEarned = await _diamondRewardService.TryGrantDailyLoginBonusAsync(user, cancellationToken);
        int trustScoreEarned = await _trustScoreService.AddDailyLoginBonusAsync(userId, cancellationToken);

        if (diamondEarned == 0 && trustScoreEarned == 0)
        {
            throw new ConflictApiException("Bạn đã điểm danh hôm nay rồi.");
        }

        // Fetch user again to get updated values if needed, but since they are updated by reference:
        return new AttendanceResponseDto
        {
            DiamondsEarned = diamondEarned,
            TrustScoreEarned = trustScoreEarned,
            CurrentDiamonds = user.Diamonds,
            CurrentTrustScore = user.TrustScore
        };
    }
}
