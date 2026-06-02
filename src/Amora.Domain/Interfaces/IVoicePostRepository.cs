using Amora.Domain.Entities;

namespace Amora.Domain.Interfaces;

public interface IVoicePostRepository
{
    Task<VoicePost?> GetByIdAsync(Guid postId, CancellationToken cancellationToken = default);

    Task<int> CountByPosterSinceAsync(Guid posterId, DateTimeOffset since, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<VoicePost> Items, int TotalCount)> GetFeedPageAsync(
        Guid viewerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<VoicePost> Items, int TotalCount)> GetMyPostsPageAsync(
        Guid posterId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task AddAsync(VoicePost post, CancellationToken cancellationToken = default);

    Task UpdateAsync(VoicePost post, CancellationToken cancellationToken = default);
}