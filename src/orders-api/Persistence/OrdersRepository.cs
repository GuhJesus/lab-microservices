using Microsoft.EntityFrameworkCore;
using orders_api.Domain;

namespace orders_api.Persistence;

public sealed class OrdersRepository : IOrdersRepository
{
    private readonly OrdersDbContext _dbContext;

    public OrdersRepository(OrdersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return _dbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(order => order.Id == id, cancellationToken);
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
