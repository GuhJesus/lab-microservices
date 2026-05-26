using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using orders_api.Orders;
using RabbitMQ.Client;
using System.Diagnostics;

namespace orders_api.Messaging;

public sealed class RabbitMqOrderCreatedPublisher : IOrderCreatedPublisher
{
    private static readonly ActivitySource ActivitySource = new("orders-api");
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqOrderCreatedPublisher> _logger;

    public RabbitMqOrderCreatedPublisher(
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqOrderCreatedPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishAsync(OrderCreatedMessage message, CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password
        };

        var payload = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(message);
        var delay = TimeSpan.FromSeconds(Math.Max(1, _options.PublishRetryDelaySeconds));
        var retryCount = Math.Max(1, _options.PublishRetryCount);

        for (var attempt = 1; attempt <= retryCount; attempt++)
        {
            try
            {
                using var activity = ActivitySource.StartActivity("orders.created publish", ActivityKind.Producer);
                activity?.SetTag("messaging.system", "rabbitmq");
                activity?.SetTag("messaging.destination.name", _options.QueueName);
                activity?.SetTag("messaging.operation", "publish");
                activity?.SetTag("order.id", message.Id);

                await using var connection = await factory.CreateConnectionAsync(cancellationToken);
                await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

                await channel.QueueDeclareAsync(
                    queue: _options.QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    cancellationToken: cancellationToken);

                var properties = new BasicProperties
                {
                    Persistent = true,
                    ContentType = "application/json",
                    Headers = new Dictionary<string, object?>()
                };

                var propagationContext = new PropagationContext(activity?.Context ?? Activity.Current?.Context ?? default, Baggage.Current);
                Propagator.Inject(propagationContext, properties, InjectTraceContextIntoBasicProperties);

                await channel.BasicPublishAsync(
                    exchange: string.Empty,
                    routingKey: _options.QueueName,
                    mandatory: false,
                    basicProperties: properties,
                    body: payload,
                    cancellationToken: cancellationToken);

                _logger.LogInformation(
                    "Evento OrderCreated publicado. OrderId={OrderId} Queue={QueueName}",
                    message.Id,
                    _options.QueueName);

                return;
            }
            catch (Exception ex) when (attempt < retryCount && !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    ex,
                    "Falha ao publicar OrderCreated. Tentativa {Attempt}/{RetryCount}. Nova tentativa em {DelaySeconds}s.",
                    attempt,
                    retryCount,
                    delay.TotalSeconds);

                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Falha definitiva ao publicar OrderCreated. OrderId={OrderId} Queue={QueueName}",
                    message.Id,
                    _options.QueueName);

                throw;
            }
        }
    }

    private static void InjectTraceContextIntoBasicProperties(IBasicProperties properties, string key, string value)
    {
        properties.Headers ??= new Dictionary<string, object?>();
        properties.Headers[key] = value;
    }
}
