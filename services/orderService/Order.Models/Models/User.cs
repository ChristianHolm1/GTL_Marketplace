using System.ComponentModel.DataAnnotations;

namespace Order.Models.Models;

public class User
{
    public Guid UserId { get; set; } = new Guid();
    [Required, EmailAddress]
    public string Email { get; set; }
    [Required]
    public string SecuredPassword { get; set; }
    [Required]
    public string Name { get; set; }
    [Required]
    public string Address { get; set; }
    [Required]
    public string City { get; set; }
    [Required]
    public string PostalCode { get; set; }
    [Required]
    public string Country { get; set; }
    [Required]
    public string Phone { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public List<OrderEntity> Orders { get; set; } = new List<OrderEntity>();
}