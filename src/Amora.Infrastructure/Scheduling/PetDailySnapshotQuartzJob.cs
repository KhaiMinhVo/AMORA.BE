using Amora.Application.Features.Pets.Commands;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Amora.Infrastructure.Scheduling;

[DisallowConcurrentExecution]
public sealed class PetDailySnapshotQuartzJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PetDailySnapshotQuartzJob> _logger;

    public PetDailySnapshotQuartzJob(IServiceScopeFactory scopeFactory, ILogger<PetDailySnapshotQuartzJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var count = await mediator.Send(new ApplyDailyPetSnapshotCommand(), context.CancellationToken);
        _logger.LogInformation("Pet daily snapshot job processed {Count} pet(s).", count);
    }
}
