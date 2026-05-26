using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using orders_api.Domain;

namespace orders_api.Caching;

public sealed class RedisOrderCache : IOrderCache
{
    private readonly IDistributedCache _cache;
    private readonly RedisOptions _options;
    private readonly ILogger<RedisOrderCache> _logger;

    public RedisOrderCache(
        IDistributedCache cache,
        IOptions<RedisOptions> options,
        ILogger<RedisOrderCache> logger)
    {
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Order?> GetAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var payload = await _cache.GetStringAsync(GetKey(id), cancellationToken);
            if (string.IsNullOrWhiteSpace(payload))
            {
                return null;
            }

            var cachedOrder = JsonSerializer.Deserialize<CachedOrder>(payload);
            return cachedOrder?.ToDomain();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao consultar cache Redis para OrderId={OrderId}. Seguindo com fallback.", id);
            return null;
        }
    }

    public async Task SetAsync(Order order, CancellationToken cancellationToken)
    {
        try
        {
            var payload = JsonSerializer.Serialize(CachedOrder.FromDomain(order));
            var cacheEntryOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(Math.Max(1, _options.OrderTtlMinutes))
            };

            await _cache.SetStringAsync(GetKey(order.Id), payload, cacheEntryOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao gravar cache Redis para OrderId={OrderId}. Seguindo sem cache.", order.Id);
        }
    }

    private string GetKey(int orderId) => $"{_options.KeyPrefix}:orders:{orderId}";

    private sealed record CachedOrder(
        int Id,
        string Code,
        string CustomerName,
        decimal Total,
        string Status,
        DateTime CreatedAt)
    {
        public static CachedOrder FromDomain(Order order) =>
            new(order.Id, order.Code, order.CustomerName, order.Total, order.Status, order.CreatedAt);

        public Order ToDomain() => new(Id, Code, CustomerName, Total, Status, CreatedAt);
    }
}
