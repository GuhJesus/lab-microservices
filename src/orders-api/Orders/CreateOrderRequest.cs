namespace orders_api.Orders;

public sealed record CreateOrderRequest(string? CustomerName, decimal Total);
