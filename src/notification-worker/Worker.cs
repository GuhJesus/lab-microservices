using System.Diagnostics;
using System.Text;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace notification_worker;

public class Worker : BackgroundService
{
    private static readonly ActivitySource ActivitySource = new("notification-worker");
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "rabbitmq",
            Port = int.TryParse(Environment.GetEnvironmentVariable("RABBITMQ_PORT"), out var port) ? port : 5672,
            UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest",
            Password = Environment.GetEnvironmentVariable("RABBITMQ_PASS") ?? "guest"
        };

        IConnection? connection = null;
        IChannel? channel = null;

        while (!stoppingToken.IsCancellationRequested && (connection is null || channel is null))
        {
            try
            {
                connection = await factory.CreateConnectionAsync(stoppingToken);
                channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RabbitMQ indisponivel. Nova tentativa em 5s...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        if (connection is null || channel is null)
        {
            return;
        }

        const string queueName = "orders.created";
        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var parentContext = Propagator.Extract(default, ea.BasicProperties, ExtractTraceContextFromBasicProperties);

            using var activity = ActivitySource.StartActivity(
                "orders.created consume",
                ActivityKind.Consumer,
                parentContext.ActivityContext);

            activity?.SetTag("messaging.system", "rabbitmq");
            activity?.SetTag("messaging.destination.name", queueName);
            activity?.SetTag("messaging.operation", "process");

            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            _logger.LogInformation("Evento recebido e processado: {Message}", message);
            await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
        };

        await channel.BasicConsumeAsync(queueName, false, consumer, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        await channel.CloseAsync(stoppingToken);
        await connection.CloseAsync(stoppingToken);
    }

    private static IEnumerable<string> ExtractTraceContextFromBasicProperties(IReadOnlyBasicProperties properties, string key)
    {
        if (properties.Headers is null || !properties.Headers.TryGetValue(key, out var value) || value is null)
        {
            return [];
        }

        return value switch
        {
            byte[] bytes => [Encoding.UTF8.GetString(bytes)],
            string text => [text],
            _ => []
        };
    }
}
