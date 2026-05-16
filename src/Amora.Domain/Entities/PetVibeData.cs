namespace Amora.Domain.Entities;

/// <summary>
/// Lưu trữ dữ liệu "Vibe" trích xuất từ giọng nói để khởi tạo / cập nhật Pet.
/// Quan hệ 1-1 với VoicePost.
/// </summary>
public sealed class PetVibeData
{
    public Guid Id { get; set; }

    /// <summary>FK → VoicePost.Id</summary>
    public Guid PostId { get; set; }

    /// <summary>RMS trung bình — độ mạnh mẽ / sôi nổi của giọng nói.</summary>
    public double Energy { get; set; }

    /// <summary>Cao độ cơ bản trung bình (Hz) — giọng trầm hay bổng.</summary>
    public double Pitch { get; set; }

    /// <summary>Phương sai cao độ — nói có ngữ điệu hay đọc đều đều.</summary>
    public double PitchVariance { get; set; }

    /// <summary>True nếu giọng nói đơn điệu (PitchVariance &lt; 500).</summary>
    public bool IsMonotone { get; set; }

    /// <summary>Độ dài file âm thanh sau khi xử lý (giây).</summary>
    public double DurationSec { get; set; }

    /// <summary>URL file âm thanh đã lọc nhiễu trên S3.</summary>
    public string CleanAudioUrl { get; set; } = string.Empty;

    public DateTimeOffset ProcessedAt { get; set; } = DateTimeOffset.UtcNow;

    public VoicePost Post { get; set; } = null!;
}
