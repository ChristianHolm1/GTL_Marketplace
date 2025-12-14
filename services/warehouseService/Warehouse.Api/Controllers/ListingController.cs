using Microsoft.AspNetCore.Mvc;
using Warehouse.Helper;
using Warehouse.Infrastructure.Cache;
using Warehouse.Infrastructure.Message;
using Warehouse.Infrastructure.Repositories;
using Warehouse.Models.Models;

namespace Warehouse.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ListingController : ControllerBase
{
    private readonly IBookRepository _repo;
    private readonly RedisCache _cache;
    private readonly IMessagePublisher _publisher;

    public ListingController(IBookRepository repo, RedisCache cache, IMessagePublisher publisher)
    {
        _repo = repo;
        _cache = cache;
        _publisher = publisher;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateListingRequest req)
    {
        if (req == null || req.Listing == null || string.IsNullOrWhiteSpace(req.Isbn))
            return BadRequest("isbn and listing are required");
        if (req.Listing.Stock <= 0)
            return BadRequest("isbn and listing are required");

        var isbn = req.Isbn;

        var book = await _repo.GetByISBNAsync(isbn);

        if (book == null) return BadRequest("book with isbn not found");

        book.Listing.Add(req.Listing);
        BookProcessor.Process(book);

        await _repo.UpdateAsync(book);
        await _cache.SetAsync(book);
        await _publisher.PublishUpdatedAsync(book);


        return CreatedAtAction("Create", new { isbn = req.Isbn }, req);
    }
    public class CreateListingRequest
    {
        public string Isbn { get; set; } = default!;
        public Listing Listing { get; set; } = default!;
    }

}
