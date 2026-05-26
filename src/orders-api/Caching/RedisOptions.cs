namespace orders_api.Caching;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    public string Connection { get; init; } = "localhost:6379";
    public string KeyPrefix { get; init; } = "orders-api";
    public int OrderTtlMinutes { get; init; } = 5;
}
