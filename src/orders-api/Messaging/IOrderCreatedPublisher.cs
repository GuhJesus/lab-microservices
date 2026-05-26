using orders_api.Orders;

namespace orders_api.Messaging;

public interface IOrderCreatedPublisher
{
    Task PublishAsync(OrderCreatedMessage message, CancellationToken cancellationToken);
}
