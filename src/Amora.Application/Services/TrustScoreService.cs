using Amora.Domain.Entities;
using Amora.Domain.Interfaces;
using Amora.Application.Exceptions;

namespace Amora.Application.Services;

public sealed class TrustScoreService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserBanRepository _userBanRepository;

    public TrustScoreService(IUserRepository userRepository, IUserBanRepository userBanRepository)
    {
        _userRepository = userRepository;
        _userBanRepository = userBanRepository;
    }

    public async Task AddProfileCompletionBonusAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdForUpdateAsync(userId, cancellationToken)
            ?? throw new NotFoundApiException("User not found.");

        if (!user.ProfileBonusClaimed && user.IsProfileComplete)
        {
            user.TrustScore = Math.Min(150, user.TrustScore + 20);
            user.ProfileBonusClaimed = true;
            await _userRepository.UpdateAsync(user, cancellationToken);
        }
    }

    public async Task<int> AddDailyLoginBonusAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdForUpdateAsync(userId, cancellationToken);
        if (user == null) return 0;

        var today = Amora.Application.Common.TimeHelper.GetVietnamToday();
        if (user.LastDailyBonus == null || user.LastDailyBonus < today)
        {
            var oldScore = user.TrustScore;
            user.TrustScore = Math.Min(150, user.TrustScore + 2);
            user.LastDailyBonus = today;
            await _userRepository.UpdateAsync(user, cancellationToken);
            return user.TrustScore - oldScore;
        }
        
        return 0;
    }

    public async Task AddVoicePostBonusAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdForUpdateAsync(userId, cancellationToken);
        if (user == null) return;

        user.TrustScore = Math.Min(150, user.TrustScore + 5);
        await _userRepository.UpdateAsync(user, cancellationToken);
    }

    public async Task DeductReportPenaltyAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdForUpdateAsync(userId, cancellationToken);
        if (user == null) return;

        user.TrustScore = Math.Max(0, user.TrustScore - 30);
        await _userRepository.UpdateAsync(user, cancellationToken);
        await CheckAndApplyBanAsync(user, cancellationToken);
    }

    public async Task DeductUnmatchPenaltyAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdForUpdateAsync(userId, cancellationToken);
        if (user == null) return;

        user.TrustScore = Math.Max(0, user.TrustScore - 20);
        await _userRepository.UpdateAsync(user, cancellationToken);
        await CheckAndApplyBanAsync(user, cancellationToken);
    }

    private async Task CheckAndApplyBanAsync(AppUser user, CancellationToken cancellationToken)
    {
        if (user.TrustScore <= 0 && !user.IsBanned)
        {
            user.IsBanned = true;
            await _userRepository.UpdateAsync(user, cancellationToken);

            var ban = new UserBan
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                BanReason = "[SYSTEM AUTOMATED] Banned due to Trust Score dropping to 0.",
                BannedUntil = DateTimeOffset.UtcNow.AddYears(100), // Ban permanently
                CreatedAt = DateTimeOffset.UtcNow,
                IsActive = true
            };
            await _userBanRepository.AddAsync(ban, cancellationToken);
        }
    }
}
