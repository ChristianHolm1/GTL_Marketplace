using Order.Models.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace Order.Infrastructure.Cache;

public class RedisCache
{
    private readonly IDatabase _db;
    private readonly TimeSpan _ttl;
    public RedisCache(IConnectionMultiplexer conn, TimeSpan ttl)
    {
        _db = conn.GetDatabase();
        _ttl = ttl;
    }

    private string Key(Guid id) => $"order:{id}";

    public async Task<OrderEntity?> GetAsync(Guid id)
    {
        var v = await _db.StringGetAsync(Key(id));
        if (!v.HasValue) return null;
        return JsonSerializer.Deserialize<OrderEntity>(v!);
    }

    public async Task SetAsync(OrderEntity order)
    {
        await _db.StringSetAsync(Key(order.OrderId), JsonSerializer.Serialize(order), _ttl);
    }

    public async Task RemoveAsync(Guid id)
    {
        await _db.KeyDeleteAsync(Key(id));
    }
}
