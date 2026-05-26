namespace notification_worker.Observability;

public sealed class OpenTelemetryOptions
{
    public const string SectionName = "OpenTelemetry";

    public string ServiceName { get; init; } = "notification-worker";
    public bool EnableConsoleExporter { get; init; } = true;
}
