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

    public async Task PurchaseSubscriptionAsync(Guid userId, SubscriptionType type, int durationDays, int priceDiamonds, CancellationToken cancellationToken)
    {
        if (type == SubscriptionType.Free)
        {
            throw new ValidationApiException("Cannot purchase free subscription.");
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundApiException("User not found.");

        if (user.Diamonds < priceDiamonds)
        {
            throw new ValidationApiException("Not enough Diamonds.");
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
}
