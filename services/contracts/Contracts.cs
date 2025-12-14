namespace Contracts.Books;
public record OrderCreatedMessage
{
    public required string ISBN { get; init; }
    public required Listing Listing { get; init; }
}

public record Listing
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = default!;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string Condition { get; set; } = default!;
}