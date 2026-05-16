namespace Amora.Application.Dtos.Iap;

public sealed class VerifyIapPurchaseRequest
{
    public string Platform { get; init; } = string.Empty;

    public string ProductId { get; init; } = string.Empty;

    public string TransactionId { get; init; } = string.Empty;

    public string ReceiptOrToken { get; init; } = string.Empty;
}

public sealed class VerifyIapPurchaseResponse
{
    public int AmoraGemsBalance { get; init; }

    public int GemsGranted { get; init; }
}
