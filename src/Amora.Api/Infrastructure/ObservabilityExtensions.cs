using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Amora.Api.Infrastructure;

public static class ObservabilityExtensions
{
    private const string ServiceName = "Amora.Api";

    public static WebApplicationBuilder AddAmoraObservability(this WebApplicationBuilder builder)
    {
        // ── Serilog ─────────────────────────────────────────────────────────
        // Configured via appsettings.json "Serilog" section (see UseSerilog below)
        // Must call builder.Host.UseSerilog() in Program.cs after this

        // ── OpenTelemetry ───────────────────────────────────────────────────
        var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService(
                    serviceName: ServiceName,
                    serviceVersion: typeof(ObservabilityExtensions).Assembly
                        .GetName().Version?.ToString() ?? "1.0.0");
                resource.AddAttributes(new[]
                {
                    new KeyValuePair<string, object>("deployment.environment",
                        builder.Environment.EnvironmentName)
                });
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(opts =>
                    {
                        // Don't trace health/metrics endpoints
                        opts.Filter = ctx =>
                            !ctx.Request.Path.StartsWithSegments("/health")
                            && !ctx.Request.Path.StartsWithSegments("/metrics");
                    })
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation(opts =>
                    {
                        opts.SetDbStatementForText = true;
                    });

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(opts =>
                    {
                        opts.Endpoint = new Uri(otlpEndpoint);
                    });
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddPrometheusExporter();
            });

        return builder;
    }

    public static WebApplication MapAmoraObservability(this WebApplication app)
    {
        // Prometheus metrics endpoint: GET /metrics
        app.MapPrometheusScrapingEndpoint("/metrics");

        return app;
    }
}
