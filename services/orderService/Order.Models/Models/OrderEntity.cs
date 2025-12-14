namespace Order.Models.Models;

public class OrderEntity
{
    public Guid OrderId { get; set; }
    public BookEntity Book { get; set; }
    public int TotalPrice { get; set; }
    public int PurchaseAmount { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;
}
