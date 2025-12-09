using Search.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Search.Application.Interfaces;

public interface ISearchService
{
    Task IndexAsync(BookDto b);
    Task UpdateAsync(BookDto b);
    Task DeleteAsync(string id);
    Task<BookDto?> GetByIdAsync(string id);
    Task<IEnumerable<BookDto>> SearchAsync(string query, int from = 0, int size = 10);
}
