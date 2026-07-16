using Amora.Application.Abstractions;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Amora.Infrastructure.Scheduling;

/// <summary>
/// Handshake 24h sweep job.
/// </summary>
public sealed class HandshakeExpiryQuartzJob : IJob
{
    private const int BatchSize = 100;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HandshakeExpiryQuartzJob> _logger;

    public HandshakeExpiryQuartzJob(IServiceScopeFactory scopeFactory, ILogger<HandshakeExpiryQuartzJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;

        try
        {
            await SweepExpiredMatchesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "HandshakeExpiryQuartzJob encountered an error during sweep.");
        }
    }

    private async Task SweepExpiredMatchesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var matchRepository = scope.ServiceProvider.GetRequiredService<IMatchConnectionRepository>();
        var chatMessageRepository = scope.ServiceProvider.GetRequiredService<IChatMessageRepository>();
        var realtimeNotifier = scope.ServiceProvider.GetRequiredService<IRealtimeNotifier>();

        var expiredMatches = await matchRepository.GetExpiredMatchesAsync(BatchSize, cancellationToken);

        if (expiredMatches.Count == 0) return;

        _logger.LogInformation("Handshake 24h: expiring {Count} match(es).", expiredMatches.Count);

        var matchIds = expiredMatches.Select(m => m.Id).ToList();
        var expiredCount = await matchRepository.ExpireMatchesAsync(matchIds, cancellationToken);

        _logger.LogInformation("Handshake 24h: {ExpiredCount} match(es) marked as Expired.", expiredCount);

        foreach (var match in expiredMatches)
        {
            try
            {
                var systemMessage = new ChatMessage
                {
                    Id = Guid.NewGuid().ToString("N")[..24], // 24 hex chars for MongoDB ObjectId
                    MatchId = match.Id,
                    SenderId = null,
                    MessageType = MessageType.System,
                    Content = "⏰ Match đã hết hạn do không có tin nhắn nào trong 24 giờ. Hãy thử kết nối với người khác nhé!",
                    CreatedAt = DateTimeOffset.UtcNow
                };

                await chatMessageRepository.AddAsync(systemMessage, cancellationToken);
                await realtimeNotifier.NotifyMatchExpiredAsync(match, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to notify users about expired match {MatchId}.", match.Id);
            }
        }
    }
}
