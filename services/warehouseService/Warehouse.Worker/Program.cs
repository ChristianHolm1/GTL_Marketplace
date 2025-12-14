using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prometheus;
using StackExchange.Redis;
using Warehouse.Infrastructure.Cache;
using Warehouse.Infrastructure.Message;
using Warehouse.Infrastructure.Repositories;
using Warehouse.Worker.Consumers;

namespace Warehouse.Worker;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                var cfg = context.Configuration;
                var pgConn = cfg.GetValue<string>("POSTGRES_CONNECTION") ?? "Host=postgres;Port=5432;Database=warehouse;Username=postgres;Password=postgres";
                var redisConn = cfg.GetValue<string>("REDIS_CONNECTION") ?? "redis:6379";
                var rabbitHost = cfg.GetValue<string>("RABBIT_HOST") ?? "rabbitmq";

                services.AddSingleton<IBookRepository>(_ => new BookRepository(pgConn));

                services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(redisConn));
                services.AddSingleton(sp => new RedisCache(sp.GetRequiredService<IConnectionMultiplexer>(), TimeSpan.FromSeconds(cfg.GetValue<int?>("REDIS_TTL_SECONDS") ?? 300)));

                services.AddMassTransit(x =>
                {
                    x.AddConsumer<OrderCreatedConsumer>();

                    x.UsingRabbitMq((context, cfg) =>
                    {
                        cfg.Host(rabbitHost);

                        cfg.ReceiveEndpoint("warehouse.order.create", e =>
                        {
                            e.ConfigureConsumer<OrderCreatedConsumer>(context);
                        });
                    });
                });


                services.AddSingleton<IMessagePublisher, MessagePublisher>();
            })
            .Build();

        var configuration = host.Services.GetRequiredService<IConfiguration>();
        var metricsPort = configuration.GetValue<int?>("METRICS_PORT") ?? 9184;

        var metricServer = new KestrelMetricServer(port: metricsPort);
        metricServer.Start();

        await host.RunAsync();
    }
}
