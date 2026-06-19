using Amora.Domain.Entities;
using Amora.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Amora.Application.Iap;

public sealed class IapWebhookService
{
    private readonly IIapPurchaseRepository _iapRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPetTransactionRepository _transactionRepository;
    private readonly IIapWebhookEventRepository _webhookEventRepository;
    private readonly ILogger<IapWebhookService> _logger;
    private readonly Services.AdminNotificationService _adminNotificationService;

    public IapWebhookService(
        IIapPurchaseRepository iapRepository,
        IUserRepository userRepository,
        IPetTransactionRepository transactionRepository,
        IIapWebhookEventRepository webhookEventRepository,
        ILogger<IapWebhookService> logger,
        Services.AdminNotificationService adminNotificationService)
    {
        _iapRepository = iapRepository;
        _userRepository = userRepository;
        _transactionRepository = transactionRepository;
        _webhookEventRepository = webhookEventRepository;
        _logger = logger;
        _adminNotificationService = adminNotificationService;
    }

    /// <summary>
    /// Check idempotency and log webhook event. Returns true if this eventId was already processed (duplicate).
    /// </summary>
    public async Task<bool> TryRecordWebhookEventAsync(
        string platform,
        string eventId,
        string eventType,
        string? transactionId,
        string? rawPayload,
        CancellationToken cancellationToken)
    {
        if (await _webhookEventRepository.ExistsAsync(platform, eventId, cancellationToken))
        {
            _logger.LogInformation("Duplicate webhook event {Platform}:{EventId}, skipping.", platform, eventId);
            return true;
        }

        await _webhookEventRepository.AddAsync(new IapWebhookEvent
        {
            Id = Guid.NewGuid(),
            Platform = platform,
            EventId = eventId,
            EventType = eventType,
            TransactionId = transactionId,
            Processed = true,
            RawPayload = rawPayload?.Length > 4000 ? rawPayload[..4000] : rawPayload,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        await _webhookEventRepository.SaveChangesAsync(cancellationToken);

        return false;
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

        var user = await _userRepository.GetByIdForUpdateAsync(record.UserId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("Refund webhook received for missing user {UserId}.", record.UserId);
            return false;
        }

        var refundable = Math.Min(record.GemsGranted, user.Diamonds);
        var delta = -refundable;

        user.Diamonds += delta;
        await _userRepository.UpdateAsync(user, cancellationToken);

        record.RefundedAt = DateTimeOffset.UtcNow;
        record.RefundReason = reason;
        record.UpdatedAt = DateTimeOffset.UtcNow;

        await _transactionRepository.AddAsync(new PetTransaction
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TransactionType = "IapRefund",
            DiamondsDelta = delta,
            MetadataJson = $"{{\"platform\":\"{platform}\",\"transactionId\":\"{transactionId}\",\"reason\":\"{reason ?? ""}\"}}",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        // Single SaveChanges — all mutations in one DB transaction
        await _iapRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Refund processed for {Platform}:{TransactionId}. Deducted {Delta} gems from user {UserId}.",
            platform, transactionId, refundable, user.Id);
            
        await _adminNotificationService.NotifySystemAlertAsync(
            "Có giao dịch bị Refund/Thu hồi", 
            $"Hệ thống đã thu hồi {delta} Kim cương từ người dùng {user.Id.ToString()[..8]} do refund giao dịch {transactionId}.", 
            cancellationToken);

        return true;
    }

    public async Task HandleRenewalAsync(string platform, string transactionId, CancellationToken cancellationToken)
    {
        var record = await _iapRepository.GetByPlatformTransactionIdAsync(platform, transactionId, cancellationToken);
        if (record is null)
        {
            // Renewal for a transaction we haven't seen yet — this can happen if
            // the initial purchase was processed on a different instance or
            // the client-side verify was skipped. Log and return.
            _logger.LogWarning("Renewal webhook received for unknown transaction {Platform}:{TransactionId}.", platform, transactionId);
            
            await _adminNotificationService.NotifySystemAlertAsync(
                "Lỗi giao dịch Webhook (Không tìm thấy)", 
                $"Nhận được webhook Renewal cho giao dịch {platform}:{transactionId} nhưng không có trong CSDL.", 
                cancellationToken);
            
            return;
        }

        // For subscription renewals, we could extend subscription expiry,
        // grant bonus gems, or update subscription status.
        // Currently Amora uses one-time gem packs, so renewal is logged for audit.
        _logger.LogInformation(
            "Renewal webhook processed for {Platform}:{TransactionId}, user {UserId}, product {ProductId}.",
            platform, transactionId, record.UserId, record.ProductId);
    }

    public Task HandleSubscriptionCanceledAsync(string platform, string transactionId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Subscription canceled: {Platform}:{TransactionId}.", platform, transactionId);
        return Task.CompletedTask;
    }
}
