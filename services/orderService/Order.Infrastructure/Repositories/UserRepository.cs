using Dapper;
using Npgsql;
using Order.Infrastructure.Interfaces;
using Order.Models.Models;
using System.Data;
using System.Text.Json;

namespace Order.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly string _connectionString;

        public UserRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        private IDbConnection CreateConn() => new NpgsqlConnection(_connectionString);

        public async Task<User?> GetByIdAsync(Guid id)
        {
            const string sql = @"SELECT payload FROM users_json WHERE user_id = @Id";
            using var conn = CreateConn();
            var payload = await conn.QuerySingleOrDefaultAsync<string?>(sql, new { Id = id });
            if (payload == null) return null;
            return JsonSerializer.Deserialize<User>(payload);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            const string sql = @"SELECT payload FROM users_json WHERE (payload ->> 'Email') = @Email LIMIT 1";
            using var conn = CreateConn();
            var payload = await conn.QuerySingleOrDefaultAsync<string?>(sql, new { Email = email });
            if (payload == null) return null;
            return JsonSerializer.Deserialize<User>(payload);
        }

        public async Task CreateAsync(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (user.UserId == Guid.Empty) user.UserId = Guid.NewGuid();
            user.CreatedAt = user.CreatedAt == default ? DateTimeOffset.UtcNow : user.CreatedAt;

            const string sql = @"
                INSERT INTO users_json (user_id, payload, updated_at)
                VALUES (@UserId::uuid, @Payload::jsonb, @UpdatedAt);
                ";
            var payload = JsonSerializer.Serialize(user);
            using var conn = CreateConn();
            await conn.ExecuteAsync(sql, new { UserId = user.UserId, Payload = payload, UpdatedAt = user.CreatedAt.UtcDateTime });
        }

        public async Task UpdateAsync(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (user.UserId == Guid.Empty) throw new ArgumentException("UserId required", nameof(user.UserId));

            user.UpdatedAt = DateTimeOffset.UtcNow;

            const string sql = @"
                INSERT INTO users_json (user_id, payload, updated_at)
                VALUES (@UserId::uuid, @Payload::jsonb, @UpdatedAt)
                ON CONFLICT (user_id)
                  DO UPDATE SET payload = EXCLUDED.payload, updated_at = EXCLUDED.updated_at;
                ";
            var payload = JsonSerializer.Serialize(user);
            using var conn = CreateConn();
            await conn.ExecuteAsync(sql, new { UserId = user.UserId, Payload = payload, UpdatedAt = user.UpdatedAt.UtcDateTime });
        }

        public async Task DeleteAsync(Guid id)
        {
            const string sql = @"DELETE FROM users_json WHERE user_id = @Id";
            using var conn = CreateConn();
            await conn.ExecuteAsync(sql, new { Id = id });
        }
    }
}
