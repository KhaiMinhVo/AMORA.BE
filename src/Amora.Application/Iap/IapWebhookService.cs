using Amora.Domain.Entities;
using Amora.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Amora.Application.Iap;

public sealed class IapWebhookService
{
    private readonly IIapPurchaseRepository _iapRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPetTransactionRepository _transactionRepository;
    private readonly ILogger<IapWebhookService> _logger;

    public IapWebhookService(
        IIapPurchaseRepository iapRepository,
        IUserRepository userRepository,
        IPetTransactionRepository transactionRepository,
        ILogger<IapWebhookService> logger)
    {
        _iapRepository = iapRepository;
        _userRepository = userRepository;
        _transactionRepository = transactionRepository;
        _logger = logger;
    }

    public async Task<bool> HandleRefundAsync(string platform, string transactionId, string? reason, CancellationToken cancellationToken)
    {
        var record = await _iapRepository.GetByPlatformTransactionIdAsync(platform, transactionId, cancellationToken);
        if (record is null)
        {
            _logger.LogWarning("Refund webhook received for unknown transaction {Platform}:{TransactionId}.", platform, transactionId);
            return false;
        }

        if (record.RefundedAt is not null)
        {
            _logger.LogInformation("Refund already processed for {Platform}:{TransactionId}.", platform, transactionId);
            return true;
        }

        var user = await _userRepository.GetByIdAsync(record.UserId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("Refund webhook received for missing user {UserId}.", record.UserId);
            return false;
        }

        var refundable = Math.Min(record.GemsGranted, user.AmoraGems);
        var delta = -refundable;

        user.AmoraGems += delta;
        await _userRepository.UpdateAsync(user, cancellationToken);

        record.RefundedAt = DateTimeOffset.UtcNow;
        record.RefundReason = reason;
        record.UpdatedAt = DateTimeOffset.UtcNow;

        await _transactionRepository.AddAsync(new PetTransaction
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TransactionType = "IapRefund",
            AmoraGemsDelta = delta,
            MetadataJson = $"{{\"platform\":\"{platform}\",\"transactionId\":\"{transactionId}\",\"reason\":\"{reason ?? ""}\"}}",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        await _iapRepository.SaveChangesAsync(cancellationToken);
        await _transactionRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public Task HandleRenewalAsync(string platform, string transactionId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Renewal webhook received for {Platform}:{TransactionId}.", platform, transactionId);
        return Task.CompletedTask;
    }
}
