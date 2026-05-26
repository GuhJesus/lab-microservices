namespace orders_api.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMQ";

    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string UserName { get; init; } = "guest";
    public string Password { get; init; } = "guest";
    public string QueueName { get; init; } = "orders.created";
    public int PublishRetryCount { get; init; } = 3;
    public int PublishRetryDelaySeconds { get; init; } = 2;
}
