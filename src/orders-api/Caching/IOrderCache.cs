using orders_api.Domain;

namespace orders_api.Caching;

public interface IOrderCache
{
    Task<Order?> GetAsync(int id, CancellationToken cancellationToken);
    Task SetAsync(Order order, CancellationToken cancellationToken);
}
