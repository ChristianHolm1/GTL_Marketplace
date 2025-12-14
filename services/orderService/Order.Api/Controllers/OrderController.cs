using Microsoft.AspNetCore.Mvc;
using Order.Infrastructure.Cache;
using Order.Infrastructure.Interfaces;
using Order.Models.Models;
using System.ComponentModel.DataAnnotations;

namespace Order.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly IOrderRepository _repo;
    private readonly RedisCache _cache;
    private readonly IMessagePublisher _publisher;

    public OrderController(IOrderRepository repo, RedisCache cache, IMessagePublisher publisher)
    {
        _repo = repo;
        _cache = cache;
        _publisher = publisher;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var cached = await _cache.GetAsync(id);
        if (cached != null) return Ok(cached);

        var order = await _repo.GetByIdAsync(id);
        if (order == null) return NotFound();
        await _cache.SetAsync(order);
        return Ok(order);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest createOrderRequest)
    {
        var order = createOrderRequest.order;
        int purchaseAmount = createOrderRequest.PurchaseAmount;

        var listing = createOrderRequest.Listing;
        if (order == null || string.IsNullOrWhiteSpace(order.OrderId.ToString())) return BadRequest("order id required");
        order.UpdatedAt = DateTimeOffset.UtcNow;

        if (purchaseAmount > listing.Stock)
            return BadRequest("buying more than in stock");

        listing.Stock = listing.Stock - purchaseAmount;

        //set order values
        order.TotalPrice = listing.Price * purchaseAmount;
        order.Book.Listings = new List<Listing> { listing };
        order.PurchaseAmount = purchaseAmount;

        await _repo.CreateAsync(order);
        await _cache.SetAsync(order);
        await _publisher.PublishCreatedOrderAsync(order.Book.ISBN, listing);
        return CreatedAtAction(nameof(Get), new { id = order.OrderId.ToString() }, order);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] OrderEntity order)
    {
        if (order == null || id != order.OrderId) return BadRequest();
        order.UpdatedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(order);
        await _cache.SetAsync(order);
        return Ok(order);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _repo.DeleteAsync(id);
        await _cache.RemoveAsync(id);
        return NoContent();
    }

    [HttpGet("batch")]
    public async Task<IActionResult> BatchGet([FromQuery] Guid ids)
    {
        if (string.IsNullOrWhiteSpace(ids.ToString())) return BadRequest();
        var arr = ids.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var results = new List<OrderEntity>();
        foreach (var id in arr)
        {
            var b = await _cache.GetAsync(Guid.Parse(id)) ?? await _repo.GetByIdAsync(Guid.Parse(id));
            if (b != null) results.Add(b);
        }

        return Ok(results);
    }

    public class CreateOrderRequest
    {
        [Required]
        public OrderEntity order { get; set; } = default!;

        [Required]
        public Listing Listing { get; set; } = default!;
        [Required]
        public int PurchaseAmount { get; set; } = default!;
    }
}
