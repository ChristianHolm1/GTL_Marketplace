using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Search.Infrastructure.Interfaces;
using Search.Models.DTOs;

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Search.Worker;

public class QueueConsumerHostedService : BackgroundService
{
    private readonly IConfiguration _cfg;
    private readonly IServiceProvider _services;
    private readonly ILogger<QueueConsumerHostedService> _logger;

    private IConnection? _conn;
    private IModel? _channel;

    private const string ExchangeName = "events";
    private const string QueueName = "books.modify";
    private const string RoutingKey = "books.modify";
    private const string ErrorQueueName = "books.modify.errors";

    public QueueConsumerHostedService(IConfiguration cfg, IServiceProvider services, ILogger<QueueConsumerHostedService> logger)
    {
        _cfg = cfg;
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var host = _cfg["RABBIT_HOST"] ?? "localhost";
        var factory = new ConnectionFactory() { HostName = host, DispatchConsumersAsync = true };

        int attempt = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                attempt++;
                _logger.LogInformation("Attempting RabbitMQ connect to {Host} (attempt {Attempt})", host, attempt);
                _conn = factory.CreateConnection();
                _conn.ConnectionShutdown += OnConnectionShutdown;
                _channel = _conn.CreateModel();

                // ensure exchange/queue/binding exist (idempotent)
                _channel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true);
                _channel.QueueDeclare(queue: QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
                _channel.QueueBind(queue: QueueName, exchange: ExchangeName, routingKey: RoutingKey);

                // Ensure error queue exists
                _channel.QueueDeclare(queue: ErrorQueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

                _logger.LogInformation("Declared/bound queue {Queue} -> {Exchange} ({Key}), error queue: {ErrorQueue}", QueueName, ExchangeName, RoutingKey, ErrorQueueName);

                // setup consumer
                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.Received += OnMessageReceivedAsync;

                _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);

                _logger.LogInformation("Consumer started for queue {Queue}", QueueName);

                while (!stoppingToken.IsCancellationRequested && _conn != null && _conn.IsOpen)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ContinueWith(_ => { });
                }

                _logger.LogWarning("RabbitMQ connection closed, will attempt to reconnect.");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RabbitMQ connect/consume failed on attempt {Attempt}", attempt);
            }
            finally
            {
                try { _channel?.Close(); } catch { }
                try { _conn?.Close(); } catch { }

                _channel?.Dispose();
                _conn?.Dispose();

                _channel = null;
                _conn = null;
            }

            var delaySeconds = Math.Min(1 + (int)Math.Pow(2, attempt), 30);
            var jitter = new Random().Next(0, 500);
            var delay = TimeSpan.FromMilliseconds(delaySeconds * 1000 + jitter);
            _logger.LogInformation("Waiting {Delay} before next RabbitMQ reconnect attempt", delay);
            await Task.Delay(delay, stoppingToken).ContinueWith(_ => { });
        }

        _logger.LogInformation("Background consumer stopping (cancellation requested).");
    }

    private void OnConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
        _logger.LogWarning("RabbitMQ connection shutdown: {ReplyText} (InitiatedByApplication={Initiated})", e.ReplyText, e.Initiator == ShutdownInitiator.Application);
    }

    private async Task OnMessageReceivedAsync(object? sender, BasicDeliverEventArgs ea)
    {
        if (_channel == null)
        {
            _logger.LogWarning("Received message but channel is null; rejecting deliveryTag {DeliveryTag}", ea.DeliveryTag);
            return;
        }

        string raw = Encoding.UTF8.GetString(ea.Body.ToArray());
        _logger.LogInformation("Raw message body received: {RawBody}", raw);

        try
        {
            // Unwrap string-encoded JSON if necessary
            string innerJson = raw;
            if (raw.StartsWith("\"") && raw.EndsWith("\""))
            {
                try
                {
                    innerJson = JsonSerializer.Deserialize<string>(raw) ?? raw;
                    _logger.LogInformation("Detected wrapped JSON, unwrapped to: {Inner}", innerJson);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to unwrap string-encoded JSON; will try original payload.");
                    innerJson = raw;
                }
            }

            // Handle envelope containing "payload" string (common shapes)
            if (innerJson.TrimStart().StartsWith("{"))
            {
                try
                {
                    using var doc = JsonDocument.Parse(innerJson);
                    if (doc.RootElement.TryGetProperty("payload", out var p) && p.ValueKind == JsonValueKind.String)
                    {
                        var possible = p.GetString();
                        if (!string.IsNullOrEmpty(possible))
                        {
                            _logger.LogInformation("Detected envelope with 'payload' string — unwrapping.");
                            innerJson = possible;
                        }
                    }
                }
                catch (Exception) { /* ignore; proceed to parse as BookDto */ }
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dto = JsonSerializer.Deserialize<BookDto>(innerJson, options);

            if (dto == null)
            {
                _logger.LogWarning("Deserialized BookDto is null for deliveryTag {DeliveryTag}; moving message to error queue.", ea.DeliveryTag);
                PublishToErrorQueue(raw, ea.BasicProperties, "Deserialized BookDto was null");
                // Nack without requeue so it is removed from primary queue
                _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                return;
            }

            _logger.LogInformation("Processing book id={Id} (title={Title})", dto.Id, dto.Title);
            using var scope = _services.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IElasticBookRepository>();
            await repo.IndexBookAsync(dto);

            _channel.BasicAck(ea.DeliveryTag, multiple: false);
            _logger.LogInformation("Message acknowledged for id={Id}", dto.Id);
        }
        catch (JsonException jex)
        {
            // Malformed JSON -> not transient
            _logger.LogWarning(jex, "JSON deserialization error — moving to error queue and NACKing without requeue for deliveryTag {DeliveryTag}", ea.DeliveryTag);
            PublishToErrorQueue(raw, ea.BasicProperties, jex.Message);
            _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
        }
        catch (Exception ex)
        {
            // For any other exception, treat as permanent for now: move to error queue and don't requeue.
            _logger.LogError(ex, "Error processing message; moving to error queue and NACKing without requeue for deliveryTag {DeliveryTag}", ea.DeliveryTag);
            PublishToErrorQueue(raw, ea.BasicProperties, ex.Message);
            try
            {
                _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            }
            catch (Exception nackEx)
            {
                _logger.LogError(nackEx, "Failed to Nack message deliveryTag {DeliveryTag}", ea.DeliveryTag);
            }
        }
    }

    private void PublishToErrorQueue(string body, IBasicProperties? originalProps, string? error = null)
    {
        if (_channel == null) return;

        try
        {
            var propsOut = _channel.CreateBasicProperties();
            propsOut.Persistent = true;

            // copy headers if any
            var headers = new Dictionary<string, object?>();
            if (originalProps?.Headers != null)
            {
                foreach (var kv in originalProps.Headers)
                {
                    headers[kv.Key] = kv.Value;
                }
            }

            headers["failedAt"] = DateTime.UtcNow.ToString("o");
            if (!string.IsNullOrEmpty(error)) headers["error"] = error;

            propsOut.Headers = headers;

            var bytes = Encoding.UTF8.GetBytes(body);
            _channel.BasicPublish(exchange: "", routingKey: ErrorQueueName, basicProperties: propsOut, body: bytes);

            _logger.LogInformation("Published failed message to error queue {ErrorQueueName}", ErrorQueueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to error queue {ErrorQueueName}", ErrorQueueName);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping QueueConsumerHostedService...");

        try { _channel?.Close(); } catch (Exception ex) { _logger.LogDebug(ex, "Error closing channel"); }
        try { _conn?.Close(); } catch (Exception ex) { _logger.LogDebug(ex, "Error closing connection"); }

        _channel?.Dispose();
        _conn?.Dispose();

        _channel = null;
        _conn = null;

        await base.StopAsync(cancellationToken);
        _logger.LogInformation("QueueConsumerHostedService stopped.");
    }
}
