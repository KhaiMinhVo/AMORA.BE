using Amora.Domain.Entities;

namespace Amora.Domain.Interfaces;

public interface IVoiceCommentRepository
{
    Task AddAsync(VoiceComment comment, CancellationToken cancellationToken = default);

    Task<VoiceComment?> GetByIdAsync(Guid commentId, CancellationToken cancellationToken = default);

    Task<bool> HasUserCommentedOnPostAsync(Guid userId, Guid postId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<VoiceComment> Items, int TotalCount)> GetPagedByPostIdAsync(
        Guid postId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(VoiceComment comment, CancellationToken cancellationToken = default);

    Task DeleteAsync(VoiceComment comment, CancellationToken cancellationToken = default);

    Task<Dictionary<DateOnly, int>> GetDailyCountsAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default);
}