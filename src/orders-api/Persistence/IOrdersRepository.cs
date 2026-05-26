using orders_api.Domain;

namespace orders_api.Persistence;

public interface IOrdersRepository
{
    Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task AddAsync(Order order, CancellationToken cancellationToken);
}
