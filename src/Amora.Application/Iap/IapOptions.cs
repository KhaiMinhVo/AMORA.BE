namespace Amora.Application.Iap;

public sealed class IapOptions
{
    public const string SectionName = "Iap";

    public bool AllowDevBypass { get; set; }

    public string? AppleSharedSecret { get; set; }

    public string? AppleBundleId { get; set; }

    public string ApplePlatform { get; set; } = "Apple";

    public string GooglePackageName { get; set; } = "vn.com.amora.app";

    public string? GoogleServiceAccountJsonPath { get; set; }

    public string? GoogleWebhookAudience { get; set; }

    public string? GoogleWebhookServiceAccountEmail { get; set; }

    public string GooglePlatform { get; set; } = "Google";

    /// <summary>productId → số Amora Gem.</summary>
    public Dictionary<string, int> Products { get; set; } = new();
}
