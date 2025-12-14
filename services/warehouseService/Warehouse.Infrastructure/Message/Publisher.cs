using MassTransit;
using Search.Models.DTOs;
using Warehouse.Models.Models;

namespace Warehouse.Infrastructure.Message
{
    public interface IMessagePublisher
    {
        Task PublishCreatedAsync(BookEntity book);
        Task PublishUpdatedAsync(BookEntity book);
        Task PublishDeletedAsync(string ISBN, DateTimeOffset updatedAt);
    }

    public class MessagePublisher : IMessagePublisher
    {
        private readonly ISendEndpointProvider _sendEndpointProvider;

        public MessagePublisher(ISendEndpointProvider sendEndpointProvider)
        {
            _sendEndpointProvider = sendEndpointProvider;
        }

        private BookDto Map(BookEntity book)
        {
            return new BookDto
            {
                ISBN = book.ISBN,
                Title = book.Title ?? string.Empty,
                Authors = book.Authors ?? new List<string>(),
                Description = book.Description,
                Categories = book.Categories ?? new List<string>(),
                QuantityAvailable = book.TotalStock,
                Tags = book.Tags ?? new List<string>(),
                UpdatedAt = book.UpdatedAt
            };
        }

        public async Task PublishCreatedAsync(BookEntity book)
        {
            var dto = Map(book);
            var ep = await _sendEndpointProvider.GetSendEndpoint(new Uri("queue:search.book.create"));
            await ep.Send(dto);
        }

        public async Task PublishUpdatedAsync(BookEntity book)
        {
            var dto = Map(book);
            var ep = await _sendEndpointProvider.GetSendEndpoint(new Uri("queue:search.book.update"));
            await ep.Send(dto);
        }

        public async Task PublishDeletedAsync(string ISBN, DateTimeOffset updatedAt)
        {
            var dto = new BookDto
            {
                ISBN = ISBN,
                Title = string.Empty,
                Authors = new List<string>(),
                Description = null,
                Categories = new List<string>(),
                QuantityAvailable = 0,
                Tags = new List<string>(),
                UpdatedAt = updatedAt
            };

            var ep = await _sendEndpointProvider.GetSendEndpoint(new Uri("queue:search.book.delete"));
            await ep.Send(dto);
        }
    }
}
