using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace notification_worker.Observability;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkerObservability(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(OpenTelemetryOptions.SectionName).Get<OpenTelemetryOptions>()
            ?? new OpenTelemetryOptions();

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(options.ServiceName))
            .WithTracing(tracing =>
            {
                tracing.AddSource("notification-worker");

                if (options.EnableConsoleExporter)
                {
                    tracing.AddConsoleExporter();
                }
            });

        return services;
    }
}
