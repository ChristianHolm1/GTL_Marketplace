using Order.Models.Models;

namespace Order.Infrastructure.Interfaces;
public interface IMessagePublisher
{
    Task PublishCreatedOrderAsync(string ISBN, Listing listing);

}