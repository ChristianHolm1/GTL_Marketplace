using Contracts.Books;
using MassTransit;
using Order.Infrastructure.Interfaces;

namespace Order.Infrastructure.Message;


public class MessagePublisher : IMessagePublisher
{
    private readonly ISendEndpointProvider _sendEndpointProvider;

    public MessagePublisher(ISendEndpointProvider sendEndpointProvider)
    {
        _sendEndpointProvider = sendEndpointProvider;
    }
    public async Task PublishCreatedOrderAsync(string _isbn, Models.Models.Listing _listing)
    {
        Contracts.Books.Listing listingToSend = new Contracts.Books.Listing
        {
            Condition = _listing.Condition,
            Price = _listing.Price,
            Id = _listing.Id,
            Stock = _listing.Stock,
            UserId = _listing.UserId,

        };

        var dto = new OrderCreatedMessage
        {
            ISBN = _isbn,
            Listing = listingToSend,
        };

        var ep = await _sendEndpointProvider.GetSendEndpoint(new Uri("queue:warehouse.order.create"));
        await ep.Send(dto);
    }
}
