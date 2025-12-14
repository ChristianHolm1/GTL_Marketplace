using Dapper;
using Npgsql;
using Order.Infrastructure.Interfaces;
using Order.Models.Models;
using System.Data;
using System.Text.Json;

namespace Order.Infrastructure.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly string _connectionString;

        public OrderRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        private IDbConnection CreateConn() => new NpgsqlConnection(_connectionString);

        public async Task<OrderEntity?> GetByIdAsync(Guid id)
        {
            const string sql = @"SELECT payload FROM orders_json WHERE order_id = @Id";
            using var conn = CreateConn();
            var payload = await conn.QuerySingleOrDefaultAsync<string?>(sql, new { Id = id });
            if (payload == null) return null;
            return JsonSerializer.Deserialize<OrderEntity>(payload);
        }

        public async Task CreateAsync(OrderEntity order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));
            if (order.OrderId == Guid.Empty) order.OrderId = Guid.NewGuid();
            order.CreatedAt = order.CreatedAt == default ? DateTimeOffset.UtcNow : order.CreatedAt;
            order.UpdatedAt = order.UpdatedAt == default ? order.CreatedAt : order.UpdatedAt;

            const string sql = @"
                INSERT INTO orders_json (order_id, payload, updated_at)
                VALUES (@OrderId::uuid, @Payload::jsonb, @UpdatedAt);
                ";
            var payload = JsonSerializer.Serialize(order);
            using var conn = CreateConn();
            await conn.ExecuteAsync(sql, new { OrderId = order.OrderId, Payload = payload, UpdatedAt = order.UpdatedAt.UtcDateTime });
        }

        public async Task UpdateAsync(OrderEntity order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));
            if (order.OrderId == Guid.Empty) throw new ArgumentException("OrderId required", nameof(order.OrderId));

            order.UpdatedAt = DateTimeOffset.UtcNow;

            const string sql = @"
                INSERT INTO orders_json (order_id, payload, updated_at)
                VALUES (@OrderId::uuid, @Payload::jsonb, @UpdatedAt)
                ON CONFLICT (order_id)
                  DO UPDATE SET payload = EXCLUDED.payload, updated_at = EXCLUDED.updated_at;
                ";
            var payload = JsonSerializer.Serialize(order);
            using var conn = CreateConn();
            await conn.ExecuteAsync(sql, new { OrderId = order.OrderId, Payload = payload, UpdatedAt = order.UpdatedAt.UtcDateTime });
        }

        public async Task DeleteAsync(Guid id)
        {
            const string sql = @"DELETE FROM orders_json WHERE order_id = @Id";
            using var conn = CreateConn();
            await conn.ExecuteAsync(sql, new { Id = id });
        }
    }
}
