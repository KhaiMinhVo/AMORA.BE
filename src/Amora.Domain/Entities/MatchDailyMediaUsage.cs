using Amora.Domain.Common;

namespace Amora.Domain.Entities;

/// <summary>Giới hạn gửi ảnh theo ngày / match (enforce stage Sprout+).</summary>
public sealed class MatchDailyMediaUsage : BaseEntity
{
    public Guid MatchId { get; set; }

    public Guid UserId { get; set; }

    public DateOnly UsageDate { get; set; }

    public int ImagesSent { get; set; }
}
