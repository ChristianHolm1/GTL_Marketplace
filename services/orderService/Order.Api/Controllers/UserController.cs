using Isopoh.Cryptography.Argon2;
using Microsoft.AspNetCore.Mvc;
using Order.Infrastructure.Interfaces;
using Order.Models.Models;
using System.ComponentModel.DataAnnotations;

namespace user.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserRepository _repo;

    public UserController(IUserRepository repo)
    {
        _repo = repo;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var user = await _repo.GetByIdAsync(id);
        if (user == null) return NotFound();
        return Ok(user);
    }
    [HttpGet("email/{email}")]
    public async Task<IActionResult> GetUserByEmail(string email)
    {
        var user = await _repo.GetByEmailAsync(email);
        if (user == null) return NotFound();
        return Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest req)
    {
        if (req is null)
            return BadRequest("Request body is required.");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = req.User;

        if (user.UserId == Guid.Empty)
            user.UserId = Guid.NewGuid();

        user.SecuredPassword = Argon2.Hash(req.Password);
        user.CreatedAt = DateTimeOffset.UtcNow;
        await _repo.CreateAsync(user);
        user.SecuredPassword = null!;
        return CreatedAtAction(nameof(Get), new { id = user.UserId }, user);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] User user)
    {
        if (id != user.UserId)
            return BadRequest("User ID mismatch.");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existing = await _repo.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        user.SecuredPassword = existing.SecuredPassword;

        await _repo.UpdateAsync(user);

        user.SecuredPassword = null!;

        return Ok(user);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _repo.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("batch")]
    public async Task<IActionResult> BatchGet([FromQuery] Guid ids)
    {
        if (string.IsNullOrWhiteSpace(ids.ToString())) return BadRequest();
        var arr = ids.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var results = new List<User>();
        foreach (var id in arr)
        {
            var b = await _repo.GetByIdAsync(Guid.Parse(id));
            if (b != null) results.Add(b);
        }

        return Ok(results);
    }

    public class CreateUserRequest
    {
        [Required]
        public User User { get; set; } = default!;

        [Required]
        public string Password { get; set; } = default!;
    }
}
