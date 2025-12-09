using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;

using Search.Infrastructure.Interfaces;
using Search.Models.DTOs;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Search.Infrastructure.Repositories;


public class ElasticBookRepository : IElasticBookRepository
{
    private readonly ElasticsearchClient _client;
    public ElasticBookRepository(ElasticClientProvider prov) => _client = prov.Client;

    public Task IndexBookAsync(BookDto b) =>
        _client.IndexAsync(b, i => i.Index("offers").Id(b.Id));

    public Task UpdateBookAsync(BookDto book) =>
        _client.IndexAsync(book, i => i.Index("offers").Id(book.Id));


    public Task DeleteBookAsync(string id) =>
        _client.DeleteAsync<BookDto>(id, d => d.Index("offers"));

    public async Task<BookDto?> GetByIdAsync(string id)
    {
        var res = await _client.GetAsync<BookDto>(id, g => g.Index("offers"));
        return res.Found ? res.Source : null;
    }
    public async Task<IEnumerable<BookDto>> SearchAsync(string q, int from = 0, int size = 10)
    {
        q ??= "";
        var normalized = System.Text.RegularExpressions.Regex.Replace(q.Trim().ToLowerInvariant().Replace(".", ""), @"\s+", " ");

        var tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lastToken = tokens.Length > 0 ? tokens[^1] : string.Empty;

        SearchResponse<BookDto> res;

        if (q == "*")
        {
            res = await _client.SearchAsync<BookDto>(s => s
               .Index("offers")
               .From(from)
               .Size(size)
               .Query(qry => qry.MatchAll()));
        }
        else
        {

            res = await _client.SearchAsync<BookDto>(s => s
                .Index("offers")
                .From(from)
                .Size(size)
                .Query(qry => qry
                    .Bool(b => b
                        .Should(
                            // forgiving multi-match across title + author with fuzziness
                            sh => sh.MultiMatch(mm => mm
                                .Query(normalized)
                                .Fields(new[] { "title^3", "author^2" }) // boost title higher
                                .Fuzziness(new Fuzziness(1))
                                .Operator(Operator.Or)
                                .Type(TextQueryType.BestFields)
                            ),

                            // match phrase prefix on author (helps partial name / initials)
                            sh => sh.MatchPhrasePrefix(mpp => mpp
                                .Field("author")
                                .Query(normalized)
                                .Slop(2)    // allow small reordering / gaps
                            ),

                            // match phrase prefix on title (helps "harry pot" -> "harry potter")
                            sh => sh.MatchPhrasePrefix(mpp => mpp
                                .Field("title")
                                .Query(normalized)
                            )


                        )
                        .MinimumShouldMatch(1)
                    )
                )
            );
        }

        return res.Documents;
    }
}
