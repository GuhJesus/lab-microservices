using Microsoft.AspNetCore.Mvc;
using orders_api.Caching;
using orders_api.Domain;
using orders_api.Messaging;
using orders_api.Orders;
using orders_api.Persistence;

namespace orders_api.Controllers;

[ApiController]
[Route("orders")]
public sealed class OrdersController : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetByIdAsync(
        int id,
        [FromServices] IOrdersRepository repository,
        [FromServices] IOrderCache cache,
        CancellationToken cancellationToken)
    {
        var cachedOrder = await cache.GetAsync(id, cancellationToken);
        if (cachedOrder is not null)
        {
            return Ok(new OrderResponse(
                cachedOrder.Id,
                cachedOrder.Code,
                cachedOrder.CustomerName,
                cachedOrder.Total,
                cachedOrder.Status,
                cachedOrder.CreatedAt,
                "redis"));
        }

        var order = await repository.GetByIdAsync(id, cancellationToken);
        if (order is null)
        {
            return NotFound(new { message = $"Order {id} not found" });
        }

        await cache.SetAsync(order, cancellationToken);

        return Ok(new OrderResponse(
            order.Id,
            order.Code,
            order.CustomerName,
            order.Total,
            order.Status,
            order.CreatedAt,
            "sqlserver"));
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateOrderRequest? request,
        [FromServices] IOrdersRepository repository,
        [FromServices] IOrderCache cache,
        [FromServices] IOrderCreatedPublisher publisher,
        CancellationToken cancellationToken)
    {
        var normalizedCustomerName = string.IsNullOrWhiteSpace(request?.CustomerName)
            ? "unknown-customer"
            : request.CustomerName.Trim();

        var order = Order.Create(normalizedCustomerName, request?.Total ?? 0m);

        await repository.AddAsync(order, cancellationToken);
        await cache.SetAsync(order, cancellationToken);

        var orderCreated = new OrderCreatedMessage(
            order.Id,
            order.Code,
            order.CustomerName,
            order.Total,
            order.CreatedAt);

        await publisher.PublishAsync(orderCreated, cancellationToken);

        return Created($"/orders/{order.Id}", new
        {
            id = order.Id,
            code = order.Code,
            customerName = order.CustomerName,
            total = order.Total,
            status = order.Status,
            eventPublished = true
        });
    }
}
