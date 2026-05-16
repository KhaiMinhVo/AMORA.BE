namespace Amora.Application.Dtos.Posts;

/// <summary>
/// Payload Python Worker gửi về qua Webhook sau khi xử lý xong.
/// </summary>
public sealed class AudioProcessedPayload
{
    public Guid PostId { get; init; }

    /// <summary>"Success" hoặc "Failed"</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>URL file âm thanh đã lọc nhiễu (chỉ có khi Status = "Success").</summary>
    public string? CleanAudioUrl { get; init; }

    public PetVibeDataDto? PetVibeData { get; init; }

    public string? Error { get; init; }
}

public sealed class PetVibeDataDto
{
    public double Energy { get; init; }
    public double Pitch { get; init; }
    public double PitchVariance { get; init; }
    public bool IsMonotone { get; init; }
    public double DurationSec { get; init; }
}
