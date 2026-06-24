using Amora.Domain.Entities;

namespace Amora.Domain.Interfaces;

public interface IMatchConnectionRepository
{
    Task<(MatchConnection MatchConnection, bool PostClosed)> CreateConnectionAsync(
        Guid postId,
        Guid commentId,
        Guid posterId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MatchConnection>> GetActiveByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<MatchConnection?> GetByIdAsync(Guid matchId, CancellationToken cancellationToken = default);

    Task<bool> IsParticipantAsync(Guid matchId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Kiểm tra 2 user có match Active với nhau không (dùng cho avatar blur).</summary>
    Task<bool> AreMatchedAsync(Guid userAId, Guid userBId, CancellationToken cancellationToken = default);

    Task<int> CountMatchesCreatedTodayAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Handshake 24h: lấy danh sách match đã quá hạn mà chưa có ai nhắn.</summary>
    Task<IReadOnlyList<MatchConnection>> GetExpiredMatchesAsync(int batchSize, CancellationToken cancellationToken = default);

    /// <summary>Handshake 24h: đánh dấu các match đã hết hạn.</summary>
    Task<int> ExpireMatchesAsync(IReadOnlyList<Guid> matchIds, CancellationToken cancellationToken = default);

    /// <summary>Handshake 24h: gỡ bỏ thời hạn khi có tin nhắn đầu tiên, giúp Match trở thành vĩnh viễn.</summary>
    Task CompleteHandshakeAsync(Guid matchId, CancellationToken cancellationToken = default);

    Task<bool> UnmatchAsync(Guid matchId, Guid userId, CancellationToken cancellationToken = default);

    Task<bool> UpdateStatusAsync(Guid matchId, Amora.Domain.Enums.MatchStatus newStatus, CancellationToken cancellationToken = default);

    Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default);

    Task<int> CountTotalMatchesAsync(CancellationToken cancellationToken = default);
}