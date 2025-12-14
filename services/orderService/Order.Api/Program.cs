using MassTransit;
using Npgsql;
using Order.Infrastructure.Cache;
using Order.Infrastructure.Interfaces;
using Order.Infrastructure.Message;
using Order.Infrastructure.Repositories;
using Prometheus;
using StackExchange.Redis;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration;

var pgConn = config["POSTGRES_CONNECTION"] ?? Environment.GetEnvironmentVariable("POSTGRES_CONNECTION");
var redisConn = config["REDIS_CONNECTION"] ?? Environment.GetEnvironmentVariable("REDIS_CONNECTION");
var rabbitHost = config["RABBIT_HOST"] ?? Environment.GetEnvironmentVariable("RABBIT_HOST");
var redisTtlSecRaw = config["REDIS_TTL_SECONDS"] ?? Environment.GetEnvironmentVariable("REDIS_TTL_SECONDS");
int? redisTtlSec = null;
if (int.TryParse(redisTtlSecRaw, out var ttl)) redisTtlSec = ttl;


builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.JsonSerializerOptions.WriteIndented = false;
    o.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
});

// Health checks for the API
builder.Services.AddHealthChecks();

// MassTransit (registers bus and transport)
builder.Services.AddMassTransit(x =>
{
    // register consumers here, e.g. x.AddConsumer<MyConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitHost);
    });
});

// Repository, messaging and cache registrations
builder.Services.AddScoped<IOrderRepository>(_ => new OrderRepository(pgConn));
builder.Services.AddScoped<IUserRepository>(_ => new UserRepository(pgConn));

builder.Services.AddScoped<IMessagePublisher, MessagePublisher>();

builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConn));
builder.Services.AddSingleton(sp => new RedisCache(sp.GetRequiredService<IConnectionMultiplexer>(), TimeSpan.FromSeconds((int)redisTtlSec)));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Ensure DB schema exists (idempotent)
await EnsureDatabaseAsync(app.Services, pgConn);

// Developer + Swagger
app.UseDeveloperExceptionPage();
app.UseSwagger();
app.UseSwaggerUI();

// routing + metrics + health
app.UseRouting();

// Prometheus HTTP metrics middleware (collects http_requests_total, durations, etc.)
app.UseHttpMetrics();

// exposes /metrics for Prometheus to scrape
app.MapMetrics();

// health endpoint
app.MapHealthChecks("/health");

// controllers
app.MapControllers();

app.Run();

static async Task EnsureDatabaseAsync(IServiceProvider services, string connectionString)
{
    using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();

    const string sql = @"
        -- JSON-backed users table used by UserRepository (stores full User as jsonb)
        CREATE TABLE IF NOT EXISTS users_json (
            user_id uuid PRIMARY KEY,
            payload jsonb NOT NULL,
            updated_at timestamptz NOT NULL
        );

        -- Unique case-insensitive index on email stored inside payload ->> 'email'
        CREATE UNIQUE INDEX IF NOT EXISTS idx_users_json_email_unique
          ON users_json (lower((payload ->> 'email')));

        -- GIN index to speed jsonb queries on users payload
        CREATE INDEX IF NOT EXISTS idx_users_json_payload_gin
          ON users_json USING gin (payload);

        -- JSON-backed orders table used by OrderRepository (stores full OrderEntity as jsonb)
        CREATE TABLE IF NOT EXISTS orders_json (
            order_id uuid PRIMARY KEY,
            payload jsonb NOT NULL,
            updated_at timestamptz NOT NULL
        );

        -- GIN index for orders payload
        CREATE INDEX IF NOT EXISTS idx_orders_json_payload_gin
          ON orders_json USING gin (payload);

        -- Optional expression index on the Book.ISBN inside the order payload:
        -- useful if you ever query orders by book ISBN: (payload -> 'Book' ->> 'ISBN')
        CREATE INDEX IF NOT EXISTS idx_orders_json_book_isbn
          ON orders_json ((payload -> 'Book' ->> 'ISBN'));
        ";

    await using var cmd = conn.CreateCommand();
    cmd.CommandText = sql;
    await cmd.ExecuteNonQueryAsync();
}

