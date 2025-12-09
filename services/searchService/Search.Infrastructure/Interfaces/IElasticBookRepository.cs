using Search.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Search.Infrastructure.Interfaces;

public interface IElasticBookRepository
{
    Task IndexBookAsync(BookDto b);
    Task UpdateBookAsync(BookDto b);
    Task DeleteBookAsync(string id);
    Task<BookDto?> GetByIdAsync(string id);
    Task<IEnumerable<BookDto>> SearchAsync(string query, int from = 0, int size = 10);
}
