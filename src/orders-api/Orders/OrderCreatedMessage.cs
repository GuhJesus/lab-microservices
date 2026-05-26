namespace orders_api.Orders;

public sealed record OrderCreatedMessage(
    int Id,
    string Code,
    string CustomerName,
    decimal Total,
    DateTime CreatedAt);
