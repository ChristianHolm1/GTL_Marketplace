using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

using Search.Infrastructure.Interfaces;

using System;
using System.Text;
using System.Threading.Tasks;

namespace Search.Infrastructure.Messaging;

public class RabbitMqEventPublisher : IEventPublisher, IDisposable
{
    private readonly IConnection _conn;
    private readonly IModel _channel;
    private readonly string _exchange = "events";

    public RabbitMqEventPublisher(IConfiguration cfg)
    {
        var factory = new ConnectionFactory() { HostName = cfg["RABBIT_HOST"] ?? "localhost" };
        _conn = factory.CreateConnection();
        _channel = _conn.CreateModel();
        _channel.ExchangeDeclare(_exchange, ExchangeType.Topic, durable: true);
    }

    public Task PublishAsync(string topic, string payload)
    {
        var body = Encoding.UTF8.GetBytes(payload);
        _channel.BasicPublish(_exchange, topic, null, body);
        return Task.CompletedTask;
    }

    public void Dispose() { _channel.Dispose(); _conn.Dispose(); }
}
