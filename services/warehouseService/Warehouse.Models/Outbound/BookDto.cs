namespace Search.Models.DTOs;

public class BookDto
{
    public string ISBN { get; set; } = default!;

    public string Title { get; set; } = default!;
    public List<string> Authors { get; set; } = new();
    public string? Description { get; set; }
    public List<string> Categories { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public int QuantityAvailable { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
