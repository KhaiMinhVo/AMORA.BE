using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;
using Amora.Application.Payment.VnPay;
using Microsoft.Extensions.Options;
using Amora.Application.Services;

namespace Amora.Application.Payment;

public sealed class PaymentService
{
    private readonly IPaymentTransactionRepository _paymentRepo;
    private readonly IUserRepository _userRepo;
    private readonly VnPayConfig _vnPayConfig;
    private readonly NotificationService _notificationService;

    public PaymentService(
        IPaymentTransactionRepository paymentRepo,
        IUserRepository userRepo,
        IOptions<VnPayConfig> vnPayConfigOptions,
        NotificationService notificationService)
    {
        _paymentRepo = paymentRepo;
        _userRepo = userRepo;
        _vnPayConfig = vnPayConfigOptions.Value;
        _notificationService = notificationService;
    }

    public async Task<string> CreateVnPayUrlAsync(Guid userId, int diamonds, CancellationToken cancellationToken)
    {
        var amountVnd = diamonds * 500;
        
        var transaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AmountVnd = amountVnd,
            DiamondsReceived = diamonds,
            Provider = "VNPay",
            Status = PaymentTransactionStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _paymentRepo.AddAsync(transaction, cancellationToken);

        var vnpay = new VnPayLibrary();
        vnpay.AddRequestData("vnp_Version", "2.1.0");
        vnpay.AddRequestData("vnp_Command", "pay");
        vnpay.AddRequestData("vnp_TmnCode", _vnPayConfig.TmnCode);
        vnpay.AddRequestData("vnp_Amount", (amountVnd * 100).ToString()); // VNPay needs amount * 100
        vnpay.AddRequestData("vnp_CreateDate", transaction.CreatedAt.ToString("yyyyMMddHHmmss"));
        vnpay.AddRequestData("vnp_CurrCode", "VND");
        vnpay.AddRequestData("vnp_IpAddr", "127.0.0.1"); // In production, pass client IP
        vnpay.AddRequestData("vnp_Locale", "vn");
        vnpay.AddRequestData("vnp_OrderInfo", $"Nap {diamonds} Diamonds cho {userId}");
        vnpay.AddRequestData("vnp_OrderType", "other");
        vnpay.AddRequestData("vnp_ReturnUrl", _vnPayConfig.ReturnUrl);
        vnpay.AddRequestData("vnp_TxnRef", transaction.Id.ToString());

        var paymentUrl = vnpay.CreateRequestUrl(_vnPayConfig.BaseUrl, _vnPayConfig.HashSecret);
        return paymentUrl;
    }

    public async Task<(string RspCode, string Message, bool Success)> ProcessVnPayCallbackAsync(IDictionary<string, string> queryParams, CancellationToken cancellationToken)
    {
        var vnpay = new VnPayLibrary();
        foreach (var kv in queryParams)
        {
            if (kv.Key.StartsWith("vnp_"))
            {
                vnpay.AddResponseData(kv.Key, kv.Value);
            }
        }

        var vnp_SecureHash = queryParams.TryGetValue("vnp_SecureHash", out var hash) ? hash : "";
        var vnp_TxnRef = queryParams.TryGetValue("vnp_TxnRef", out var txn) ? txn : "";
        var vnp_ResponseCode = queryParams.TryGetValue("vnp_ResponseCode", out var code) ? code : "";
        var vnp_TransactionNo = queryParams.TryGetValue("vnp_TransactionNo", out var transNo) ? transNo : "";
        var vnp_Amount = queryParams.TryGetValue("vnp_Amount", out var amt) ? amt : "0";

        if (!vnpay.ValidateSignature(vnp_SecureHash, _vnPayConfig.HashSecret))
        {
            return ("97", "Invalid signature", false);
        }

        if (!Guid.TryParse(vnp_TxnRef, out var transactionId))
        {
            return ("01", "Order not found", false);
        }

        var transaction = await _paymentRepo.GetByIdAsync(transactionId, cancellationToken);
        if (transaction is null)
        {
            return ("01", "Order not found", false);
        }

        if (transaction.AmountVnd * 100 != long.Parse(vnp_Amount))
        {
            return ("04", "Invalid amount", false);
        }

        if (transaction.Status != PaymentTransactionStatus.Pending)
        {
            return ("02", "Order already confirmed", true);
        }

        transaction.ProviderTransactionId = vnp_TransactionNo;

        if (vnp_ResponseCode == "00")
        {
            transaction.Status = PaymentTransactionStatus.Success;
            
            var user = await _userRepo.GetByIdForUpdateAsync(transaction.UserId, cancellationToken);
            if (user is not null)
            {
                user.Diamonds += transaction.DiamondsReceived;
                await _userRepo.UpdateAsync(user, cancellationToken);
                
                // Gửi thông báo nạp kim cương thành công
                await _notificationService.SendNotificationAsync(
                    user.Id,
                    NotificationType.Payment,
                    "Nạp Kim cương thành công!",
                    $"Bạn vừa nạp thành công {transaction.DiamondsReceived} Kim cương vào tài khoản.",
                    $"{{\"transactionId\": \"{transaction.Id}\"}}",
                    cancellationToken
                );
            }
        }
        else
        {
            transaction.Status = PaymentTransactionStatus.Failed;
        }

        await _paymentRepo.UpdateAsync(transaction, cancellationToken);

        return ("00", "Confirm Success", transaction.Status == PaymentTransactionStatus.Success);
    }
}
