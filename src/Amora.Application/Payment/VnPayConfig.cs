namespace Amora.Application.Payment;

public sealed class VnPayConfig
{
    public string TmnCode { get; set; } = string.Empty;
    public string HashSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
}
