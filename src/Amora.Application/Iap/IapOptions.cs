namespace Amora.Application.Iap;

public sealed class IapOptions
{
    public const string SectionName = "Iap";

    public bool AllowDevBypass { get; set; }

    public string? AppleSharedSecret { get; set; }

    public string? GooglePackageName { get; set; }

    /// <summary>productId → số Amora Gem.</summary>
    public Dictionary<string, int> Products { get; set; } = new();
}
