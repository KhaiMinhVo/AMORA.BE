using Amora.Domain.Entities;

namespace Amora.Domain.Interfaces;

public interface IIapWebhookEventRepository
{
    Task<bool> ExistsAsync(string platform, string eventId, CancellationToken cancellationToken = default);

    Task AddAsync(IapWebhookEvent webhookEvent, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
