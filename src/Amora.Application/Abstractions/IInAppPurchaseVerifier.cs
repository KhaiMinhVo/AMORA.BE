namespace Amora.Application.Abstractions;

public sealed class IapVerificationRequest
{
    public string Platform { get; init; } = string.Empty;

    public string ProductId { get; init; } = string.Empty;

    public string TransactionId { get; init; } = string.Empty;

    /// <summary>Apple: base64 receipt. Google: purchase token.</summary>
    public string ReceiptOrToken { get; init; } = string.Empty;
}

public sealed class IapVerificationResult
{
    public bool IsValid { get; init; }

    public string? ErrorMessage { get; init; }

    public static IapVerificationResult Ok() => new() { IsValid = true };

    public static IapVerificationResult Fail(string message) => new() { IsValid = false, ErrorMessage = message };
}

/// <summary>Xác thực receipt Apple/Google — không lưu nội dung nhạy cảm.</summary>
public interface IInAppPurchaseVerifier
{
    Task<IapVerificationResult> VerifyAsync(IapVerificationRequest request, CancellationToken cancellationToken = default);
}
