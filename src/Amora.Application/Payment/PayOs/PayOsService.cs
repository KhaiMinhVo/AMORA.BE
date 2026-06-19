using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using global::PayOS;
using global::PayOS.Models;
using global::PayOS.Models.Webhooks;

namespace Amora.Application.Payment.PayOs;

public sealed class PayOsService
{
    private readonly IPaymentTransactionRepository _paymentRepo;
    private readonly IUserRepository _userRepo;
    private readonly PayOsConfig _payOsConfig;
    private readonly ILogger<PayOsService> _logger;
    private readonly global::PayOS.PayOSClient _payOsClient;

    public PayOsService(
        IPaymentTransactionRepository paymentRepo,
        IUserRepository userRepo,
        IOptions<PayOsConfig> payOsConfigOptions,
        ILogger<PayOsService> logger)
    {
        _paymentRepo = paymentRepo;
        _userRepo = userRepo;
        _payOsConfig = payOsConfigOptions.Value;
        _logger = logger;

        _payOsClient = new global::PayOS.PayOSClient(
            _payOsConfig.ClientId,
            _payOsConfig.ApiKey,
            _payOsConfig.ChecksumKey);
    }

    public async Task<string> CreatePayOsUrlAsync(Guid userId, int diamonds, CancellationToken cancellationToken)
    {
        var amountVnd = diamonds * 500;
        
        // Use current ticks for a unique positive int64 order code.
        // Ticks is 62-bits, payOS allows up to 53-bits in JS, but API accepts int64.
        // To be safe for JS max safe integer (9007199254740991), we can take timestamp in milliseconds.
        long orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 9007199254740991;
        
        var transaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AmountVnd = amountVnd,
            DiamondsReceived = diamonds,
            Provider = "PayOS",
            OrderCode = orderCode,
            Status = PaymentTransactionStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _paymentRepo.AddAsync(transaction, cancellationToken);



        var paymentRequest = new global::PayOS.Models.V2.PaymentRequests.CreatePaymentLinkRequest
        {
            OrderCode = orderCode,
            Amount = amountVnd,
            Description = $"Nap {diamonds} Diamonds",
            CancelUrl = "https://amora.pro.vn/payment/cancel",
            ReturnUrl = "https://amora.pro.vn/payment/success"
        };

        try
        {
            var paymentLink = await _payOsClient.PaymentRequests.CreateAsync(paymentRequest);
            return paymentLink.CheckoutUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create PayOS link.");
            throw;
        }
    }

    public async Task<bool> VerifyPaymentWebhookAsync(global::PayOS.Models.Webhooks.Webhook webhookBody, CancellationToken cancellationToken)
    {
        try
        {
            // Verify signature
            var data = await _payOsClient.Webhooks.VerifyAsync(webhookBody);

            if (webhookBody.Code == "00" && webhookBody.Success)
            {
                var transaction = await _paymentRepo.GetByOrderCodeAsync(data.OrderCode, cancellationToken);
                if (transaction is null)
                {
                    _logger.LogWarning($"PayOS IPN: Transaction not found {data.OrderCode}");
                    return false;
                }

                if (transaction.Status != PaymentTransactionStatus.Pending)
                {
                    // Already processed
                    return true;
                }

                if (transaction.AmountVnd != data.Amount)
                {
                    _logger.LogWarning($"PayOS IPN: Amount mismatch. Expected {transaction.AmountVnd}, got {data.Amount}");
                    return false;
                }

                // Update Status
                transaction.Status = PaymentTransactionStatus.Success;
                transaction.ProviderTransactionId = data.Reference;

                var user = await _userRepo.GetByIdForUpdateAsync(transaction.UserId, cancellationToken);
                if (user is not null)
                {
                    user.Diamonds += transaction.DiamondsReceived;
                    await _userRepo.UpdateAsync(user, cancellationToken);
                }

                await _paymentRepo.UpdateAsync(transaction, cancellationToken);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayOS Verify Webhook Error");
            return false;
        }
    }
}
