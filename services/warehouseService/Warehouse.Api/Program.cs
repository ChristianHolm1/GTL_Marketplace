using MassTransit;
using Npgsql;
using Prometheus;
using StackExchange.Redis;
using System.Text.Json;
using Warehouse.Infrastructure.Cache;
using Warehouse.Infrastructure.Message;
using Warehouse.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration;
var pgConn = config.GetValue<string>("POSTGRES_CONNECTION", "Host=postgres;Port=5432;Database=warehouse;Username=postgres;Password=postgres");
var redisConn = config.GetValue<string>("REDIS_CONNECTION", "redis:6379");
var rabbitHost = config.GetValue<string>("RABBIT_HOST", "rabbitmq");
var redisTtlSec = config.GetValue<int?>("REDIS_TTL_SECONDS") ?? 300;

builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.JsonSerializerOptions.WriteIndented = false;
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
builder.Services.AddSingleton<IBookRepository>(_ => new BookRepository(pgConn));
builder.Services.AddSingleton<IMessagePublisher, MessagePublisher>();

builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConn));
builder.Services.AddSingleton(sp => new RedisCache(sp.GetRequiredService<IConnectionMultiplexer>(), TimeSpan.FromSeconds(redisTtlSec)));

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
    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();
    const string sql = @"
        CREATE TABLE IF NOT EXISTS books (
          ISBN text PRIMARY KEY,
          payload jsonb NOT NULL,
          updated_at timestamptz NOT NULL
        );";
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = sql;
    await cmd.ExecuteNonQueryAsync();
}
