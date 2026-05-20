using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Amora.Infrastructure.HealthChecks;

public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;

    public RedisHealthCheck(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var latency = await _redis.GetDatabase().PingAsync();
            return HealthCheckResult.Healthy("Redis ping succeeded.", new Dictionary<string, object>
            {
                ["latencyMs"] = latency.TotalMilliseconds
            });
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis ping failed.", ex);
        }
    }
}
