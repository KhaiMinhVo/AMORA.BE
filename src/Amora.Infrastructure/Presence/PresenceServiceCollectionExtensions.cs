using Amora.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Amora.Infrastructure.Presence;

public static class PresenceServiceCollectionExtensions
{
    public static IServiceCollection AddPresenceTracking(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? configuration["Redis:ConnectionString"];

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
            services.AddSingleton<IMatchPresenceTracker, RedisMatchPresenceTracker>();
            services.AddSingleton<IUserPresenceTracker, InMemoryUserPresenceTracker>(); // TODO: RedisUserPresenceTracker later
            return services;
        }

        services.AddSingleton<IMatchPresenceTracker, InMemoryMatchPresenceTracker>();
        services.AddSingleton<IUserPresenceTracker, InMemoryUserPresenceTracker>();
        return services;
    }
}
