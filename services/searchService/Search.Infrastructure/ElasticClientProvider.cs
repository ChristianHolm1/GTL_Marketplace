using Elastic.Clients.Elasticsearch;

using Microsoft.Extensions.Configuration;

using System;

namespace Search.Infrastructure;

public class ElasticClientProvider
{
    public ElasticsearchClient Client { get; }
    public ElasticClientProvider(IConfiguration cfg)
    {
        var uri = cfg["ELASTIC_URI"] ?? "http://localhost:9200";
        Client = new ElasticsearchClient(new ElasticsearchClientSettings(new Uri(uri)));
    }
}
