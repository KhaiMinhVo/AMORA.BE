using Amora.Domain.Interfaces;
using Amora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Amora.Infrastructure.Repositories;

public sealed class IapWebhookEventRepository : IIapWebhookEventRepository
{
    private readonly AmoraDbContext _db;

    public IapWebhookEventRepository(AmoraDbContext db) => _db = db;

    public Task<bool> ExistsAsync(string platform, string eventId, CancellationToken cancellationToken = default)
        => _db.IapWebhookEvents.AnyAsync(
            x => x.Platform == platform && x.EventId == eventId,
            cancellationToken);

    public async Task AddAsync(Domain.Entities.IapWebhookEvent webhookEvent, CancellationToken cancellationToken = default)
        => await _db.IapWebhookEvents.AddAsync(webhookEvent, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _db.SaveChangesAsync(cancellationToken);
}
