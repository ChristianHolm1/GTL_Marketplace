using Search.Application.Interfaces;
using Search.Infrastructure.Interfaces;
using Search.Models.DTOs;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Search.Application.Services;

public class SearchService : ISearchService
{
    private readonly IElasticBookRepository _repo;
    public SearchService(IElasticBookRepository repo) => _repo = repo;

    public Task IndexAsync(BookDto b) => _repo.IndexBookAsync(b);
    public Task UpdateAsync(BookDto b) => _repo.UpdateBookAsync(b);
    public Task DeleteAsync(string id) => _repo.DeleteBookAsync(id);
    public Task<BookDto?> GetByIdAsync(string id) => _repo.GetByIdAsync(id);
    public Task<IEnumerable<BookDto>> SearchAsync(string q, int from, int size) =>
        _repo.SearchAsync(q, from, size);

}
