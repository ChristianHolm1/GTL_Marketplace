using Microsoft.AspNetCore.Mvc;

using Search.Application.Interfaces;
using Search.Infrastructure.Interfaces;
using Search.Models.DTOs;
using System.Threading.Tasks;

namespace Search.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly ISearchService _search;
    private readonly IEventPublisher _publisher;

    public BooksController(ISearchService search, IEventPublisher publisher)
    {
        _search = search; _publisher = publisher;
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int from = 0, [FromQuery] int size = 10)
    {
        return Ok(await _search.SearchAsync(q ?? "", from, size));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BookDto b)
    {
        await _search.IndexAsync(b);
        return CreatedAtAction(nameof(GetById), new { id = b.Id }, b);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var book = await _search.GetByIdAsync(id);
        return book is null ? NotFound() : Ok(book);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] BookDto b)
    {
        b.Id = id;
        await _search.UpdateAsync(b);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _search.DeleteAsync(id);
        return NoContent();
    }
}
