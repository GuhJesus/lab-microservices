namespace orders_api.Domain;

public sealed class Order
{
    public int Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string CustomerName { get; private set; } = string.Empty;
    public decimal Total { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private Order()
    {
    }

    public Order(int id, string code, string customerName, decimal total, string status, DateTime createdAt)
    {
        Id = id;
        Code = code;
        CustomerName = customerName;
        Total = total;
        Status = status;
        CreatedAt = createdAt;
    }

    public static Order Create(string customerName, decimal total)
    {
        var createdAt = DateTime.UtcNow;

        return new Order(
            id: 0,
            code: $"ORD-{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
            customerName: customerName,
            total: total,
            status: "Created",
            createdAt: createdAt);
    }
}
