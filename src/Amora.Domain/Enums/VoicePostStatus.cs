namespace Amora.Domain.Enums;

public enum VoicePostStatus
{
    /// <summary>Đang chờ Python Worker xử lý âm thanh (chưa hiện trên Feed).</summary>
    Processing = 0,

    /// <summary>Đã xử lý xong, hiện lên Feed.</summary>
    Open = 1,

    /// <summary>Đã đóng (không nhận thêm comment / match).</summary>
    Closed = 2,

    /// <summary>Worker báo lỗi — Admin cần xem xét.</summary>
    Failed = 3
}