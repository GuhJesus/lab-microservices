namespace orders_api.Observability;

public sealed class OpenTelemetryOptions
{
    public const string SectionName = "OpenTelemetry";

    public string ServiceName { get; init; } = "orders-api";
    public bool EnableConsoleExporter { get; init; } = true;
}
