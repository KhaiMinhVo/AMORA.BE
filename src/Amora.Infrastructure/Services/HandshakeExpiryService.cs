using Amora.Application.Abstractions;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Amora.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Amora.Infrastructure.Services;

/// <summary>
/// Handshake 24h — Background job quét và expire các match không có tin nhắn trong 24 giờ.
/// Chạy mỗi 5 phút, xử lý theo batch 100 match mỗi lần.
/// </summary>
public sealed class HandshakeExpiryService : BackgroundService
{
    private static readonly TimeSpan SweepInterval = TimeSpan.FromMinutes(5);
    private const int BatchSize = 100;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HandshakeExpiryService> _logger;

    public HandshakeExpiryService(IServiceScopeFactory scopeFactory, ILogger<HandshakeExpiryService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HandshakeExpiryService started — sweep interval: {Interval}", SweepInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SweepExpiredMatchesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "HandshakeExpiryService encountered an error during sweep.");
            }

            await Task.Delay(SweepInterval, stoppingToken);
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

        // Gửi system message & real-time notification cho từng match
        foreach (var match in expiredMatches)
        {
            try
            {
                var systemMessage = new ChatMessage
                {
                    Id = Guid.NewGuid().ToString("N"),
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
