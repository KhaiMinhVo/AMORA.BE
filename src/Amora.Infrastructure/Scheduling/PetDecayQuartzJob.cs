using Amora.Application.Features.Pets.Commands;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Amora.Infrastructure.Scheduling;

[DisallowConcurrentExecution]
public sealed class PetDecayQuartzJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PetDecayQuartzJob> _logger;

    public PetDecayQuartzJob(IServiceScopeFactory scopeFactory, ILogger<PetDecayQuartzJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var count = await mediator.Send(new ApplyDecayCommand(), context.CancellationToken);
        if (count > 0)
            _logger.LogInformation("Pet decay Quartz job affected {Count} pet(s).", count);
    }
}
