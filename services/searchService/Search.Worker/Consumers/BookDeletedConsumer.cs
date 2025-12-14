using MassTransit;
using Microsoft.Extensions.Logging;
using Search.Infrastructure.Interfaces;
using Search.Models.DTOs;
using System.Threading.Tasks;

namespace Search.Worker;
public class BookDeletedConsumer : IConsumer<BookDto>
{
    private readonly IElasticBookRepository _repo;
    private readonly ILogger<BookDeletedConsumer> _logger;

    public BookDeletedConsumer(IElasticBookRepository repo, ILogger<BookDeletedConsumer> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<BookDto> context)
    {
        var dto = context.Message;
        _logger.LogInformation("DELETE received ISBN={ISBN}", dto.ISBN);
        await _repo.DeleteBookAsync(dto.ISBN);
    }
}
