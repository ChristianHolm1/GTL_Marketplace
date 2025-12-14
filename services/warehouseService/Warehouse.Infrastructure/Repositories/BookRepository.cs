using Dapper;
using Npgsql;
using System.Data;
using System.Text.Json;
using Warehouse.Models.Models;

namespace Warehouse.Infrastructure.Repositories;

public class BookRepository : IBookRepository
{
    private readonly string _connectionString;
    public BookRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    private IDbConnection CreateConn() => new NpgsqlConnection(_connectionString);

    public async Task<BookEntity?> GetByISBNAsync(string ISBN)
    {
        const string sql = "SELECT payload FROM books WHERE ISBN = @ISBN";
        using var conn = CreateConn();
        var payload = await conn.QuerySingleOrDefaultAsync<string?>(sql, new { ISBN = ISBN });
        if (payload == null) return null;
        return JsonSerializer.Deserialize<BookEntity>(payload);
    }

    public async Task UpdateAsync(BookEntity book)
    {
        const string sql = @"
                INSERT INTO books (ISBN, payload, updated_at)
                VALUES (@ISBN, @Payload::jsonb, @UpdatedAt)
                ON CONFLICT (ISBN)
                  DO UPDATE SET payload = EXCLUDED.payload, updated_at = EXCLUDED.updated_at;
                ";
        using var conn = CreateConn();
        await conn.ExecuteAsync(sql, new { book.ISBN, Payload = JsonSerializer.Serialize(book), UpdatedAt = book.UpdatedAt.UtcDateTime });
    }

    public async Task DeleteAsync(string ISBN)
    {
        const string sql = "DELETE FROM books WHERE ISBN = @ISBN";
        using var conn = CreateConn();
        await conn.ExecuteAsync(sql, new { ISBN = ISBN });
    }

    public async Task CreateAsync(BookEntity book)
    {
        const string sql = @"
            INSERT INTO books (ISBN, payload, updated_at)
            VALUES (@ISBN, @Payload::jsonb, @UpdatedAt);
            ";

        using var conn = CreateConn();
        await conn.ExecuteAsync(sql, new
        {
            book.ISBN,
            Payload = JsonSerializer.Serialize(book),
            UpdatedAt = book.UpdatedAt.UtcDateTime
        });
    }
}
