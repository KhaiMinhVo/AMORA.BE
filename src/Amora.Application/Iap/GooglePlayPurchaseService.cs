using Amora.Application.Abstractions;
using Amora.Application.Exceptions;
using Amora.Domain.Entities;
using Amora.Domain.Interfaces;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Amora.Application.Iap;

public sealed class GooglePlayPurchaseService
{
    private readonly IGooglePlayPurchaseVerifier _verifier;
    private readonly IIapPurchaseRepository _iapRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPaymentTransactionRepository _transactionRepository;
    private readonly IapOptions _options;
    private readonly ILogger<GooglePlayPurchaseService> _logger;

    public GooglePlayPurchaseService(
        IGooglePlayPurchaseVerifier verifier,
        IIapPurchaseRepository iapRepository,
        IUserRepository userRepository,
        IPaymentTransactionRepository transactionRepository,
        IOptions<IapOptions> options,
        ILogger<GooglePlayPurchaseService> logger)
    {
        _verifier = verifier;
        _iapRepository = iapRepository;
        _userRepository = userRepository;
        _transactionRepository = transactionRepository;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Verify with Google Play and credit diamonds inside a single DB Transaction.
    /// Uses purchaseToken as idempotency key.
    /// </summary>
    public async Task<int> ProcessPurchaseAsync(Guid userId, string productId, string purchaseToken, CancellationToken cancellationToken)
    {
        if (!_options.Products.TryGetValue(productId, out var gems))
            throw new ValidationApiException($"Không tìm thấy sản phẩm: {productId}");

        // Start DB Transaction implicitly via single SaveChanges on DbContext
        // Idempotency check: if purchaseToken already exists, return current balance (success)
        if (await _iapRepository.ExistsAsync(_options.GooglePlatform, purchaseToken, cancellationToken))
        {
            _logger.LogInformation("Google Purchase Token {Token} already processed. Idempotency return.", purchaseToken);
            var currentUser = await _userRepository.GetByIdAsync(userId, cancellationToken)
                ?? throw new NotFoundApiException("Không tìm thấy người dùng.");
            return currentUser.Diamonds;
        }

        // Verify with Google Play
        var verifyRequest = new IapVerificationRequest
        {
            Platform = _options.GooglePlatform,
            ProductId = productId,
            TransactionId = purchaseToken,
            ReceiptOrToken = purchaseToken
        };

        var verification = await _verifier.VerifyAsync(verifyRequest, cancellationToken);
        if (!verification.IsValid)
            throw new ValidationApiException(verification.ErrorMessage ?? "Invalid Google Play purchase.");

        // Update Balance
        var user = await _userRepository.GetByIdForUpdateAsync(userId, cancellationToken)
            ?? throw new NotFoundApiException("Không tìm thấy người dùng.");

        user.Diamonds += gems;
        await _userRepository.UpdateAsync(user, cancellationToken);

        // Record IAP
        await _iapRepository.AddAsync(new IapPurchaseRecord
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Platform = _options.GooglePlatform,
            TransactionId = purchaseToken, // purchaseToken acts as TransactionId
            ProductId = productId,
            GemsGranted = gems,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        // Record Transaction
        // Assuming we need a random OrderCode for PaymentTransaction if we share it with PayOS
        var orderCode = long.Parse(DateTimeOffset.UtcNow.ToString("yyMMddHHmmssff"));
        await _transactionRepository.AddAsync(new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = "GooglePlay",
            ProviderTransactionId = purchaseToken,
            OrderCode = orderCode,
            AmountVnd = 0, // Google Play handles fiat mapping on client/console
            Status = Amora.Domain.Enums.PaymentTransactionStatus.Success,
            DiamondsReceived = gems,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        // Single SaveChanges commits the transaction
        await _iapRepository.SaveChangesAsync(cancellationToken);

        // Acknowledge Purchase via Google API (Fire and forget or await)
        _ = Task.Run(async () =>
        {
            try
            {
                var ackResult = await _verifier.AcknowledgePurchaseAsync(productId, purchaseToken, CancellationToken.None);
                if (!ackResult)
                    _logger.LogWarning("Failed to acknowledge Google Play purchase {Token}", purchaseToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error acknowledging Google Play purchase {Token}", purchaseToken);
            }
        });

        return user.Diamonds;
    }
}
