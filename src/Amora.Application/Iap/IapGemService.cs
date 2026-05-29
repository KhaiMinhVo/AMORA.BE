using Amora.Application.Abstractions;
using Amora.Application.Exceptions;
using Amora.Domain.Entities;
using Amora.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace Amora.Application.Iap;

public sealed class IapGemService
{
    private readonly IInAppPurchaseVerifier _verifier;
    private readonly IIapPurchaseRepository _iapRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPetTransactionRepository _transactionRepository;
    private readonly IapOptions _options;

    public IapGemService(
        IInAppPurchaseVerifier verifier,
        IIapPurchaseRepository iapRepository,
        IUserRepository userRepository,
        IPetTransactionRepository transactionRepository,
        IOptions<IapOptions> options)
    {
        _verifier = verifier;
        _iapRepository = iapRepository;
        _userRepository = userRepository;
        _transactionRepository = transactionRepository;
        _options = options.Value;
    }

    public async Task<int> VerifyAndCreditAsync(Guid userId, IapVerificationRequest request, CancellationToken cancellationToken)
    {
        if (!_options.Products.TryGetValue(request.ProductId, out var gems))
            throw new ValidationApiException($"Unknown product: {request.ProductId}");

        if (await _iapRepository.ExistsAsync(request.Platform, request.TransactionId, cancellationToken))
            throw new ConflictApiException("Transaction already processed.");

        var verification = await _verifier.VerifyAsync(request, cancellationToken);
        if (!verification.IsValid)
            throw new ValidationApiException(verification.ErrorMessage ?? "Invalid purchase.");

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundApiException("User not found.");

        user.Diamonds += gems;
        await _userRepository.UpdateAsync(user, cancellationToken);

        await _iapRepository.AddAsync(new IapPurchaseRecord
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Platform = request.Platform,
            TransactionId = request.TransactionId,
            ProductId = request.ProductId,
            GemsGranted = gems,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        await _transactionRepository.AddAsync(new PetTransaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TransactionType = "IapGemPurchase",
            DiamondsDelta = gems,
            MetadataJson = $"{{\"platform\":\"{request.Platform}\",\"productId\":\"{request.ProductId}\"}}",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        // Single SaveChanges — both repos share the same DbContext
        await _iapRepository.SaveChangesAsync(cancellationToken);

        return user.Diamonds;
    }
}
