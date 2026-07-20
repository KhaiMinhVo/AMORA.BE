namespace Amora.Application.Abstractions;

public interface IGooglePlayPurchaseVerifier : IInAppPurchaseVerifier
{
    Task<bool> AcknowledgePurchaseAsync(string productId, string token, CancellationToken cancellationToken);
}
