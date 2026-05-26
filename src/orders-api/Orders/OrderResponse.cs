namespace orders_api.Orders;

public sealed record OrderResponse(
    int Id,
    string Code,
    string CustomerName,
    decimal Total,
    string Status,
    DateTime CreatedAt,
    string Source);
