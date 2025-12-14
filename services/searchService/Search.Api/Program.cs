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


var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddHealthChecks();

builder.Services.AddSingleton<ElasticClientProvider>();

builder.Services.AddScoped<IElasticBookRepository, ElasticBookRepository>();
builder.Services.AddScoped<ISearchService, SearchService>();

builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();


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


app.UseHttpMetrics();
app.MapMetrics();
app.MapHealthChecks("/health");

app.MapControllers();
app.Run();
