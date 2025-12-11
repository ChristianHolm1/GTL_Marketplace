using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Prometheus;
using Search.Application.Interfaces;
using Search.Application.Services;
using Search.Infrastructure;
using Search.Infrastructure.Interfaces;
using Search.Infrastructure.Messaging;
using Search.Infrastructure.Repositories;
using Search.Worker;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
builder.Services.AddHealthChecks(); // optional if you use /health

builder.Services.AddSingleton<ElasticClientProvider>();

builder.Services.AddScoped<IElasticBookRepository, ElasticBookRepository>();
builder.Services.AddScoped<ISearchService, SearchService>();

builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

builder.Services.AddHostedService<QueueConsumerHostedService>();


builder.Services.AddControllers();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Search API", Version = "v1" });
});

var app = builder.Build();

//if (app.Environment.IsDevelopment() || Environment.GetEnvironmentVariable("ALLOW_SWAGGER_IN_CONTAINER") == "true")
//{
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Search API v1"));
//}
app.UseHttpMetrics(); // enable HTTP duration, count, etc.
app.MapMetrics();     // exposes /metrics
app.MapHealthChecks("/health"); // optional
app.MapControllers();
app.Run();
