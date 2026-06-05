using System;
using System.Threading;
using System.Threading.Tasks;
using Amora.Application.Exceptions;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;

namespace Amora.Application.Services;

public sealed class PostPromotionService
{
    private readonly IVoicePostRepository _postRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPostBoostRecordRepository _boostRecordRepository;
    private readonly IPetTransactionRepository _transactionRepository;

    public PostPromotionService(
        IVoicePostRepository postRepository,
        IUserRepository userRepository,
        IPostBoostRecordRepository boostRecordRepository,
        IPetTransactionRepository transactionRepository)
    {
        _postRepository = postRepository;
        _userRepository = userRepository;
        _boostRecordRepository = boostRecordRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task BoostPostAsync(Guid userId, Guid postId, PostBoostType boostType, int priceDiamonds, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundApiException("User not found.");

        var post = await _postRepository.GetByIdAsync(postId, cancellationToken)
            ?? throw new NotFoundApiException("Post not found.");

        if (post.PosterId != userId)
        {
            throw new ForbiddenApiException("You can only boost your own posts.");
        }

        if (user.Diamonds < priceDiamonds)
        {
            throw new ValidationApiException("Not enough Diamonds.");
        }

        user.Diamonds -= priceDiamonds;

        var record = new PostBoostRecord
        {
            Id = Guid.NewGuid(),
            PostId = postId,
            UserId = userId,
            BoostType = boostType,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) // 24h boost
        };

        await _boostRecordRepository.AddAsync(record, cancellationToken);
        await _userRepository.UpdateAsync(user, cancellationToken);

        await _transactionRepository.AddAsync(new PetTransaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ShopItemId = null,
            TransactionType = $"{boostType} Post",
            DiamondsDelta = -priceDiamonds,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        await _transactionRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task AddMatchSlotsAsync(Guid userId, Guid postId, int extraSlots, int priceDiamonds, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundApiException("User not found.");

        var post = await _postRepository.GetByIdAsync(postId, cancellationToken)
            ?? throw new NotFoundApiException("Post not found.");

        if (post.PosterId != userId)
        {
            throw new ForbiddenApiException("You can only add slots to your own posts.");
        }

        if (user.Diamonds < priceDiamonds)
        {
            throw new ValidationApiException("Not enough Diamonds.");
        }

        user.Diamonds -= priceDiamonds;
        post.MaxMatchSlots += extraSlots;

        await _postRepository.UpdateAsync(post, cancellationToken);
        await _userRepository.UpdateAsync(user, cancellationToken);

        await _transactionRepository.AddAsync(new PetTransaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ShopItemId = null,
            TransactionType = $"Add {extraSlots} Slots to Post",
            DiamondsDelta = -priceDiamonds,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        await _transactionRepository.SaveChangesAsync(cancellationToken);
    }
}
