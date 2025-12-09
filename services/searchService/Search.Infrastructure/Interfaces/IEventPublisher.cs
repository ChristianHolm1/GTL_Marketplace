using System.Threading.Tasks;

namespace Search.Infrastructure.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync(string topic, string payload);
}
