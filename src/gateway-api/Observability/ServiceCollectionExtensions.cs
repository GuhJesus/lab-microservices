using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace gateway_api.Observability;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGatewayObservability(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(OpenTelemetryOptions.SectionName).Get<OpenTelemetryOptions>()
            ?? new OpenTelemetryOptions();

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(options.ServiceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (options.EnableConsoleExporter)
                {
                    tracing.AddConsoleExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (options.EnableConsoleExporter)
                {
                    metrics.AddConsoleExporter();
                }
            });

        return services;
    }
}
