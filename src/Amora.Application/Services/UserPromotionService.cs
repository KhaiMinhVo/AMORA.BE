using System;
using System.Threading;
using System.Threading.Tasks;
using Amora.Application.Exceptions;
using Amora.Domain.Entities;
using Amora.Domain.Interfaces;

namespace Amora.Application.Services;

public sealed class UserPromotionService
{
    private readonly IUserRepository _userRepository;
    private readonly IPetTransactionRepository _transactionRepository;

    public UserPromotionService(
        IUserRepository userRepository,
        IPetTransactionRepository transactionRepository)
    {
        _userRepository = userRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task AddMatchSlotsAsync(Guid userId, int extraSlots, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdForUpdateAsync(userId, cancellationToken)
            ?? throw new NotFoundApiException("Không tìm thấy người dùng.");

        if (extraSlots <= 0 || extraSlots % 2 != 0)
        {
            throw new ValidationApiException("Số lượt mua thêm phải là số chẵn (ví dụ: 2, 4, 6...).");
        }

        int priceDiamonds = (extraSlots / 2) * 30; // 30 diamonds per 2 slots

        if (user.Diamonds < priceDiamonds)
        {
            throw new ValidationApiException("Bạn không đủ Kim Cương (Diamonds).");
        }

        user.Diamonds -= priceDiamonds;
        user.ExtraMatchSlots += extraSlots;

        await _userRepository.UpdateAsync(user, cancellationToken);

        await _transactionRepository.AddAsync(new PetTransaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ShopItemId = null,
            TransactionType = $"Add {extraSlots} Match Slots",
            DiamondsDelta = -priceDiamonds,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        await _transactionRepository.SaveChangesAsync(cancellationToken);
    }
}
