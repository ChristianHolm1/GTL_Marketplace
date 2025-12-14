using Contracts.Books;
using MassTransit;
using Warehouse.Helper;
using Warehouse.Infrastructure.Cache;
using Warehouse.Infrastructure.Message;
using Warehouse.Infrastructure.Repositories;

namespace Warehouse.Worker.Consumers;

public class OrderCreatedConsumer : IConsumer<OrderCreatedMessage>
{
    private readonly IBookRepository _repo;
    private readonly RedisCache _cache;
    private readonly IMessagePublisher _publisher;

    public OrderCreatedConsumer(IBookRepository repo, RedisCache cache, IMessagePublisher publisher)
    {
        _repo = repo;
        _cache = cache;
        _publisher = publisher;
    }

    public async Task Consume(ConsumeContext<OrderCreatedMessage> context)
    {
        var msg = context.Message;

        var book = await _repo.GetByISBNAsync(msg.ISBN);
        if (book == null)
            return;

        var listing = book.Listing.FirstOrDefault(l => l.Id == msg.Listing.Id);
        if (listing == null)
            return;

        listing.Stock = msg.Listing.Stock;

        if (listing.Stock < 0)
            listing.Stock = 0;

        BookProcessor.Process(book);

        book.UpdatedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(book);
        await _cache.SetAsync(book);
        await _publisher.PublishUpdatedAsync(book);
    }
}
