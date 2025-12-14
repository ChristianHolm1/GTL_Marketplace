using Isopoh.Cryptography.Argon2;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Order.Infrastructure.Interfaces;
using Order.Models.Models;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Order.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _repo;

    public AuthController(IUserRepository repo)
    {
        _repo = repo;
    }
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (req is null) return BadRequest("request required");
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await _repo.GetByEmailAsync(req.Email);
        if (user == null) return Unauthorized(new { message = "Invalid credentials" });
        var valid = false;
        try
        {
            valid = Argon2.Verify(user.SecuredPassword, req.Password);
        }
        catch
        {
            // If verification fails due to unknown format, you can fallback to other checks here
            valid = false;
        }

        if (!valid) return Unauthorized(new { message = "Invalid credentials" });

        var token = GenerateJwtToken(user);

        var auth = new AuthResponse
        {
            Token = token.Token,
            ExpiresAt = token.ExpiresAt,
            UserId = user.UserId,
            Email = user.Email
        };

        return Ok(auth);
    }
    private (string Token, DateTimeOffset ExpiresAt) GenerateJwtToken(User user)
    {
        //this should be in a config
        var key = "a_very_good_and_secret_key_hi_:)";
        var issuer = "issuer";
        var audience = "audience";
        var expiryMinutes = 600;

        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("JWT signing key not configured");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim("uid", user.UserId.ToString())
            };

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt.UtcDateTime,
            signingCredentials: creds
        );

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return (jwt, expiresAt);
    }
    public class LoginRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = default!;

        [Required]
        public string Password { get; set; } = default!;
    }

    public class AuthResponse
    {
        public string Token { get; set; } = default!;
        public DateTimeOffset ExpiresAt { get; set; }
        public Guid UserId { get; set; }
        public string Email { get; set; } = default!;
    }
}
