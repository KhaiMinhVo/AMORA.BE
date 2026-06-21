using System;
using System.Threading;
using System.Threading.Tasks;
using Amora.Application.Exceptions;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;

namespace Amora.Application.Services;

public sealed class SubscriptionService
{
    private readonly IUserRepository _userRepository;
    private readonly IPetTransactionRepository _transactionRepository;

    public SubscriptionService(
        IUserRepository userRepository,
        IPetTransactionRepository transactionRepository)
    {
        _userRepository = userRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task PurchaseSubscriptionAsync(Guid userId, SubscriptionType type, int durationDays, int priceDiamonds, bool enableAutoRenew, CancellationToken cancellationToken)
    {
        if (type == SubscriptionType.Free)
        {
            throw new ValidationApiException("Không thể mua gói miễn phí.");
        }

        var user = await _userRepository.GetByIdForUpdateAsync(userId, cancellationToken)
            ?? throw new NotFoundApiException("Không tìm thấy người dùng.");

        if (user.Diamonds < priceDiamonds)
        {
            throw new ValidationApiException("Bạn không đủ Kim Cương (Diamonds).");
        }

        user.Diamonds -= priceDiamonds;

        // Upgrade or extend
        if (user.SubscriptionType == type && user.SubscriptionEndDate.HasValue && user.SubscriptionEndDate.Value > DateTimeOffset.UtcNow)
        {
            user.SubscriptionEndDate = user.SubscriptionEndDate.Value.AddDays(durationDays);
        }
        else if (user.SubscriptionType != type && type == SubscriptionType.Gold && user.SubscriptionEndDate.HasValue && user.SubscriptionEndDate.Value > DateTimeOffset.UtcNow)
        {
            // Upgrading from Premium to Gold
            // Calculate remaining premium value and convert? For simplicity, we just overwrite and extend from now.
            user.SubscriptionType = type;
            user.SubscriptionEndDate = DateTimeOffset.UtcNow.AddDays(durationDays);
        }
        else
        {
            user.SubscriptionType = type;
            user.SubscriptionEndDate = DateTimeOffset.UtcNow.AddDays(durationDays);
        }

        if (enableAutoRenew)
        {
            user.IsAutoRenewEnabled = true;
            user.AutoRenewDurationDays = durationDays;
            user.AutoRenewPriceDiamonds = priceDiamonds;
        }
        else
        {
            user.IsAutoRenewEnabled = false;
        }

        await _userRepository.UpdateAsync(user, cancellationToken);

        await _transactionRepository.AddAsync(new PetTransaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ShopItemId = null,
            TransactionType = $"Buy {type} {durationDays}D",
            DiamondsDelta = -priceDiamonds,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        await _transactionRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelSubscriptionAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdForUpdateAsync(userId, cancellationToken)
            ?? throw new NotFoundApiException("Không tìm thấy người dùng.");

        if (user.SubscriptionType == SubscriptionType.Free)
        {
            throw new ValidationApiException("Bạn hiện không có gói đăng ký nào để hủy.");
        }

        var oldType = user.SubscriptionType;
        user.SubscriptionType = SubscriptionType.Free;
        user.SubscriptionEndDate = null;
        user.IsAutoRenewEnabled = false;

        await _userRepository.UpdateAsync(user, cancellationToken);

        await _transactionRepository.AddAsync(new PetTransaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ShopItemId = null,
            TransactionType = $"Cancel {oldType}",
            DiamondsDelta = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        await _transactionRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ToggleAutoRenewAsync(Guid userId, bool enable, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdForUpdateAsync(userId, cancellationToken)
            ?? throw new NotFoundApiException("Không tìm thấy người dùng.");

        if (user.SubscriptionType == SubscriptionType.Free && enable)
        {
            throw new ValidationApiException("Bạn phải có gói Premium hoặc Gold để bật tự gia hạn.");
        }

        user.IsAutoRenewEnabled = enable;
        await _userRepository.UpdateAsync(user, cancellationToken);

        return enable;
    }
}
