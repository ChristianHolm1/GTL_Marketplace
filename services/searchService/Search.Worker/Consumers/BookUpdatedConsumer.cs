using MassTransit;
using Microsoft.Extensions.Logging;
using Search.Infrastructure.Interfaces;
using Search.Models.DTOs;
using System.Threading.Tasks;

namespace Search.Worker;
public class BookUpdatedConsumer : IConsumer<BookDto>
{
    private readonly IElasticBookRepository _repo;
    private readonly ILogger<BookUpdatedConsumer> _logger;

    public BookUpdatedConsumer(IElasticBookRepository repo, ILogger<BookUpdatedConsumer> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<BookDto> context)
    {
        var dto = context.Message;
        _logger.LogInformation("UPDATE received ISBN={ISBN}", dto.ISBN);
        await _repo.UpdateBookAsync(dto);
    }
}
