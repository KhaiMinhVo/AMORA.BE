using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Amora.Infrastructure.Scheduling;

public static class QuartzServiceCollectionExtensions
{
    public static IServiceCollection AddAmoraQuartzJobs(this IServiceCollection services)
    {
        services.AddQuartz(q =>
        {
            var decayKey = new JobKey("pet-decay");
            q.AddJob<PetDecayQuartzJob>(opts => opts.WithIdentity(decayKey));
            q.AddTrigger(opts => opts
                .ForJob(decayKey)
                .WithIdentity("pet-decay-trigger")
                .WithCronSchedule("0 */15 * * * ?")); // mỗi 15 phút

            var snapshotKey = new JobKey("pet-daily-snapshot");
            q.AddJob<PetDailySnapshotQuartzJob>(opts => opts.WithIdentity(snapshotKey));
            q.AddTrigger(opts => opts
                .ForJob(snapshotKey)
                .WithIdentity("pet-daily-snapshot-trigger")
                .WithCronSchedule("0 5 0 * * ?")); // 00:05 UTC mỗi ngày
        });

        services.AddQuartzHostedService(o => o.WaitForJobsToComplete = true);
        return services;
    }
}
