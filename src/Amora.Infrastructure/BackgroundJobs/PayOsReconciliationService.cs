using Amora.Application.Payment.PayOs;
using Amora.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Amora.Infrastructure.BackgroundJobs;

public sealed class PayOsReconciliationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PayOsReconciliationService> _logger;

    public PayOsReconciliationService(IServiceProvider serviceProvider, ILogger<PayOsReconciliationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PayOS Reconciliation Service is starting.");

        // Initial delay to avoid running immediately on startup
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("PayOS Reconciliation Service is running a scan.");

                using var scope = _serviceProvider.CreateScope();
                var paymentRepo = scope.ServiceProvider.GetRequiredService<IPaymentTransactionRepository>();
                var payOsService = scope.ServiceProvider.GetRequiredService<PayOsService>();

                // Scan transactions that have been pending for at least 10 minutes to allow webhooks to process naturally
                var olderThan = DateTime.UtcNow.AddMinutes(-10);
                var pendingTransactions = await paymentRepo.GetPendingPayOsTransactionsAsync(olderThan, stoppingToken);

                if (pendingTransactions.Any())
                {
                    _logger.LogInformation($"Found {pendingTransactions.Count} pending PayOS transactions to reconcile.");

                    foreach (var transaction in pendingTransactions)
                    {
                        // Add a small delay between API calls to avoid rate limiting
                        await Task.Delay(500, stoppingToken);
                        
                        await payOsService.ReconcilePendingTransactionAsync(transaction, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while reconciling PayOS transactions.");
            }

            // Wait 30 minutes before the next scan
            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }

        _logger.LogInformation("PayOS Reconciliation Service is stopping.");
    }
}
