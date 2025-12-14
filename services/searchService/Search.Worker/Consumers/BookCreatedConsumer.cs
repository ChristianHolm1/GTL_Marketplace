using MassTransit;
using Microsoft.Extensions.Logging;
using Search.Infrastructure.Interfaces;
using Search.Models.DTOs;
using System.Threading.Tasks;

namespace Search.Worker;
public class BookCreatedConsumer : IConsumer<BookDto>
{
    private readonly IElasticBookRepository _repo;
    private readonly ILogger<BookCreatedConsumer> _logger;

    public BookCreatedConsumer(IElasticBookRepository repo, ILogger<BookCreatedConsumer> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<BookDto> context)
    {
        var dto = context.Message;
        _logger.LogInformation("CREATE received ISBN={ISBN}", dto.ISBN);
        await _repo.IndexBookAsync(dto);
    }
}
