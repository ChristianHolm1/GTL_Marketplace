using Warehouse.Models.Models;

namespace Warehouse.Infrastructure.Repositories
{
    public interface IBookRepository
    {
        Task<BookEntity?> GetByISBNAsync(string ISBN);
        Task UpdateAsync(BookEntity book);
        Task DeleteAsync(string ISBN);
        Task CreateAsync(BookEntity book);
    }
}
