using Amora.Domain.Entities;
using Amora.Domain.Interfaces;

namespace Amora.Application.Iap;

/// <summary>Cơ chế kiếm Pet Coin miễn phí.</summary>
public sealed class PetCoinRewardService
{
    public const int DailyLoginPetCoins = 15;
    public const int CoPresencePetCoins = 5;

    private readonly IUserRepository _users;

    public PetCoinRewardService(IUserRepository users) => _users = users;

    /// <summary>+15 PC mỗi ngày khi đăng nhập.</summary>
    public async Task<int> TryGrantDailyLoginBonusAsync(AppUser user, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (user.LastPetCoinRewardDate == today) return 0;

        user.LastPetCoinRewardDate = today;
        user.PetCoins += DailyLoginPetCoins;
        await _users.UpdateAsync(user, cancellationToken);
        return DailyLoginPetCoins;
    }

    /// <summary>+5 PC khi online cùng partner (gọi từ co-presence handler).</summary>
    public async Task<int> TryGrantCoPresenceCoinsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdForUpdateAsync(userId, cancellationToken);
        if (user is null) return 0;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (user.LastCoPresenceCoinDate == today) return 0;

        user.LastCoPresenceCoinDate = today;
        user.PetCoins += CoPresencePetCoins;
        await _users.UpdateAsync(user, cancellationToken);
        return CoPresencePetCoins;
    }
}
