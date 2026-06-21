using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amora.Application.Services;
using Amora.Domain.Enums;
using Amora.Infrastructure.Data;
using Amora.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Amora.Infrastructure.BackgroundJobs;

public sealed class SubscriptionAutoRenewService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SubscriptionAutoRenewService> _logger;

    public SubscriptionAutoRenewService(
        IServiceProvider serviceProvider,
        ILogger<SubscriptionAutoRenewService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SubscriptionAutoRenewService is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRenewalsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing ProcessRenewalsAsync.");
            }

            // Run every 5 minutes
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private async Task ProcessRenewalsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AmoraDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();

        var now = DateTimeOffset.UtcNow;

        // Find expired or expiring subscriptions
        // Wait exactly when it expires (SubscriptionEndDate <= now)
        var usersToProcess = await dbContext.Users
            .Where(u => u.SubscriptionType != SubscriptionType.Free
                     && u.SubscriptionEndDate <= now)
            .ToListAsync(stoppingToken);

        if (!usersToProcess.Any())
        {
            return;
        }

        foreach (var user in usersToProcess)
        {
            if (user.IsAutoRenewEnabled)
            {
                // Attempt to auto-renew
                if (user.Diamonds >= user.AutoRenewPriceDiamonds)
                {
                    // Deduct diamonds and extend
                    user.Diamonds -= user.AutoRenewPriceDiamonds;
                    user.SubscriptionEndDate = now.AddDays(user.AutoRenewDurationDays);

                    // Add transaction
                    dbContext.PetTransactions.Add(new PetTransaction
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        ShopItemId = null,
                        TransactionType = $"Auto-Renew {user.SubscriptionType} {user.AutoRenewDurationDays}D",
                        DiamondsDelta = -user.AutoRenewPriceDiamonds,
                        CreatedAt = now,
                        UpdatedAt = now
                    });

                    await notificationService.SendNotificationAsync(
                        user.Id,
                        NotificationType.System,
                        "Gia hạn thành công",
                        $"Gói {user.SubscriptionType} của bạn đã được tự động gia hạn thành công.",
                        null,
                        stoppingToken);
                }
                else
                {
                    // Insufficient diamonds, cancel subscription
                    var oldType = user.SubscriptionType;
                    user.SubscriptionType = SubscriptionType.Free;
                    user.SubscriptionEndDate = null;
                    user.IsAutoRenewEnabled = false;

                    dbContext.PetTransactions.Add(new PetTransaction
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        ShopItemId = null,
                        TransactionType = $"Auto-Renew Failed {oldType}",
                        DiamondsDelta = 0,
                        CreatedAt = now,
                        UpdatedAt = now
                    });

                    await notificationService.SendNotificationAsync(
                        user.Id,
                        NotificationType.System,
                        "Gia hạn thất bại",
                        $"Gói {oldType} của bạn đã bị hủy do số dư Kim Cương không đủ để tự động gia hạn.",
                        null,
                        stoppingToken);
                }
            }
            else
            {
                // Auto-renew disabled, just cancel subscription
                var oldType = user.SubscriptionType;
                user.SubscriptionType = SubscriptionType.Free;
                user.SubscriptionEndDate = null;

                await notificationService.SendNotificationAsync(
                    user.Id,
                    NotificationType.System,
                    "Gói đăng ký hết hạn",
                    $"Gói {oldType} của bạn đã hết hạn. Bạn đã trở về gói Free.",
                    null,
                    stoppingToken);
            }
        }

        await dbContext.SaveChangesAsync(stoppingToken);
    }
}
