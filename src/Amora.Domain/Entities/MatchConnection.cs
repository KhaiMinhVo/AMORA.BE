using Amora.Domain.Enums;

namespace Amora.Domain.Entities;

public sealed class MatchConnection
{
    public Guid Id { get; set; }

    public Guid PostId { get; set; }

    public Guid UserAId { get; set; }

    public Guid UserBId { get; set; }

    public MatchStatus Status { get; set; } = MatchStatus.Active;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Handshake 24h: thời điểm match sẽ hết hạn nếu không có tin nhắn nào được gửi.
    /// Mỗi lần gửi tin nhắn, giá trị này được đẩy thêm 24h.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; } = DateTimeOffset.UtcNow.AddHours(24);
}