namespace Amora.Application.Payment.PayOs;

public sealed class PayOsConfig
{
    public string ClientId { get; set; } = string.Empty;
    
    public string ApiKey { get; set; } = string.Empty;
    
    public string ChecksumKey { get; set; } = string.Empty;
    
    public string ReturnUrl { get; set; } = string.Empty;
    
    public string CancelUrl { get; set; } = string.Empty;
}
