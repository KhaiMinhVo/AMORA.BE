namespace Amora.Application.Abstractions;

/// <summary>Theo dõi heartbeat SignalR — phát hiện hai user online cùng match.</summary>
public interface IMatchPresenceTracker
{
    /// <summary>Cập nhật heartbeat; trả true nếu cả hai participant đang online (trong cửa sổ).</summary>
    bool RecordHeartbeat(Guid matchId, Guid userId, Guid userAId, Guid userBId);

    void RemoveConnection(Guid matchId, Guid userId);
}
