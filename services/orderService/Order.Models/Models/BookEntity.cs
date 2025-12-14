namespace Order.Models.Models;

public class BookEntity
{
    public string ISBN { get; set; } = default!;
    public string Title { get; set; } = default!;
    public List<string> Authors { get; set; } = new();
    public string? Description { get; set; }
    public List<string> Categories { get; set; } = new();
    public string? PublishedDate { get; set; }
    public IEnumerable<Listing>? Listings { get; set; }
    public int TotalStock { get; set; }
    public List<string> Tags { get; set; } = new();
    public DateTimeOffset UpdatedAt { get; set; }
}
