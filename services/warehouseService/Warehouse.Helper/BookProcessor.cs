using Warehouse.Models.Models;
namespace Warehouse.Helper;

public class BookProcessor
{
    public static BookEntity Process(BookEntity book)
    {
        UpdateTotalStock(book);
        RemoveEmptySellers(book);
        return book;
    }

    private static BookEntity UpdateTotalStock(BookEntity book)
    {
        int totalStock = book.Listing?.Sum(s => s.Stock) ?? 0; ;

        book.TotalStock = totalStock;

        return book;
    }
    private static BookEntity RemoveEmptySellers(BookEntity book)
    {
        book.Listing.RemoveAll(s => s.Stock <= 0);

        if (book.Listing.Count == 0)
        {
            book.Listing = new List<Listing>();
        }

        book.TotalStock = book.Listing.Sum(s => s.Stock);

        return book;
    }
}
