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
            throw new ValidationApiException($"Không tìm thấy sản phẩm: {request.ProductId}");

        if (await _iapRepository.ExistsAsync(request.Platform, request.TransactionId, cancellationToken))
            throw new ConflictApiException("Giao dịch này đã được xử lý trước đó.");

        var verification = await _verifier.VerifyAsync(request, cancellationToken);
        if (!verification.IsValid)
            throw new ValidationApiException(verification.ErrorMessage ?? "Invalid purchase.");

        var user = await _userRepository.GetByIdForUpdateAsync(userId, cancellationToken)
            ?? throw new NotFoundApiException("Không tìm thấy người dùng.");

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
