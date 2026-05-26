namespace gateway_api.Observability;

public sealed class OpenTelemetryOptions
{
    public const string SectionName = "OpenTelemetry";

    public string ServiceName { get; init; } = "gateway-api";
    public bool EnableConsoleExporter { get; init; } = true;
}
