using Microsoft.AspNetCore.Mvc;
using Warehouse.Helper;
using Warehouse.Infrastructure.Cache;
using Warehouse.Infrastructure.Message;
using Warehouse.Infrastructure.Repositories;
using Warehouse.Models.Models;

namespace Warehouse.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly IBookRepository _repo;
    private readonly RedisCache _cache;
    private readonly IMessagePublisher _publisher;

    public BooksController(IBookRepository repo, RedisCache cache, IMessagePublisher publisher)
    {
        _repo = repo;
        _cache = cache;
        _publisher = publisher;
    }

    [HttpGet("{ISBN}")]
    public async Task<IActionResult> Get(string ISBN)
    {
        var cached = await _cache.GetAsync(ISBN);
        if (cached != null) return Ok(cached);

        var book = await _repo.GetByISBNAsync(ISBN);
        if (book == null) return NotFound();
        await _cache.SetAsync(book);
        return Ok(book);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BookEntity book)
    {
        if (book == null || string.IsNullOrWhiteSpace(book.ISBN)) return BadRequest("Book ISBN required");
        book.UpdatedAt = DateTimeOffset.UtcNow;
        BookProcessor.Process(book);
        await _repo.CreateAsync(book);
        await _cache.SetAsync(book);
        await _publisher.PublishCreatedAsync(book);
        return CreatedAtAction(nameof(Get), new { book.ISBN }, book);
    }

    [HttpPut("{ISBN}")]
    public async Task<IActionResult> Update(string ISBN, [FromBody] BookEntity book)
    {
        if (book == null || ISBN != book.ISBN) return BadRequest();
        book.UpdatedAt = DateTimeOffset.UtcNow;
        BookProcessor.Process(book);
        await _repo.UpdateAsync(book);
        await _cache.SetAsync(book);
        await _publisher.PublishUpdatedAsync(book);
        return Ok(book);
    }

    [HttpDelete("{ISBN}")]
    public async Task<IActionResult> Delete(string ISBN)
    {
        await _repo.DeleteAsync(ISBN);
        await _cache.RemoveAsync(ISBN);
        var updatedAt = DateTimeOffset.UtcNow;
        await _publisher.PublishDeletedAsync(ISBN, updatedAt);
        return NoContent();
    }

    [HttpGet("batch")]
    public async Task<IActionResult> BatchGet([FromQuery] string ISBNs)
    {
        if (string.IsNullOrWhiteSpace(ISBNs)) return BadRequest();
        var arr = ISBNs.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var results = new List<BookEntity>();
        foreach (var ISBN in arr)
        {
            var b = await _cache.GetAsync(ISBN) ?? await _repo.GetByISBNAsync(ISBN);
            if (b != null) results.Add(b);
        }

        return Ok(results);
    }


}
