using StackExchange.Redis;
using System.Text.Json;
using Warehouse.Models.Models;

namespace Warehouse.Infrastructure.Cache
{
    public class RedisCache
    {
        private readonly IDatabase _db;
        private readonly TimeSpan _ttl;
        public RedisCache(IConnectionMultiplexer conn, TimeSpan ttl)
        {
            _db = conn.GetDatabase();
            _ttl = ttl;
        }

        private string Key(string ISBN) => $"book:{ISBN}";

        public async Task<BookEntity?> GetAsync(string ISBN)
        {
            var v = await _db.StringGetAsync(Key(ISBN));
            if (!v.HasValue) return null;
            return JsonSerializer.Deserialize<BookEntity>(v!);
        }

        public async Task SetAsync(BookEntity book)
        {
            await _db.StringSetAsync(Key(book.ISBN), JsonSerializer.Serialize(book), _ttl);
        }

        public async Task RemoveAsync(string ISBN)
        {
            await _db.KeyDeleteAsync(Key(ISBN));
        }
    }
}
