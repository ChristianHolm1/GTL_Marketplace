using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prometheus;
using Search.Infrastructure;
using Search.Infrastructure.Interfaces;
using Search.Infrastructure.Repositories;
using System.Threading.Tasks;

namespace Search.Worker;
public static class Program
{
    public static async Task Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        var rabbitHost = builder.Configuration["RABBIT_HOST"] ?? "rabbitmq";

        builder.Services.AddSingleton<ElasticClientProvider>();
        builder.Services.AddScoped<IElasticBookRepository, ElasticBookRepository>();

        builder.Services.AddMassTransit(x =>
        {
            x.AddConsumer<BookCreatedConsumer>();
            x.AddConsumer<BookUpdatedConsumer>();
            x.AddConsumer<BookDeletedConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitHost);

                cfg.ReceiveEndpoint("search.book.create", e =>
                {
                    e.ConfigureConsumer<BookCreatedConsumer>(context);
                });

                cfg.ReceiveEndpoint("search.book.update", e =>
                {
                    e.ConfigureConsumer<BookUpdatedConsumer>(context);
                });

                cfg.ReceiveEndpoint("search.book.delete", e =>
                {
                    e.ConfigureConsumer<BookDeletedConsumer>(context);
                });
            });
        });


        var host = builder.Build();

        var metricsPort = builder.Configuration.GetValue<int?>("METRICS_PORT") ?? 9184;
        var metricServer = new KestrelMetricServer(port: metricsPort);
        metricServer.Start();

        await host.RunAsync();
    }
}