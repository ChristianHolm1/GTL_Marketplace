namespace Order.Models.Models;

public class Listing
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = default!;
    public int Price { get; set; }
    public int Stock { get; set; }
    public string Condition { get; set; } = default!;
}
