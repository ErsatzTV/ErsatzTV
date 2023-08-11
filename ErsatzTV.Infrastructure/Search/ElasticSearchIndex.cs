using Bugsnag;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Repositories.Caching;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Search;
using ExistsResponse = Elastic.Clients.Elasticsearch.IndexManagement.ExistsResponse;

namespace ErsatzTV.Infrastructure.Search;

public class ElasticSearchIndex : ISearchIndex
{
    private ElasticsearchClient _client;

    public void Dispose()
    {
        // do nothing
    }

    public int Version { get; }

    public async Task<bool> Initialize(ILocalFileSystem localFileSystem, IConfigElementRepository configElementRepository)
    {
        _client = new ElasticsearchClient(new Uri("http://localhost:9200"));
        ExistsResponse exists = await _client.Indices.ExistsAsync("ersatztv");
        if (!exists.IsValidResponse || !exists.Exists)
        {
            CreateIndexResponse createResponse = await _client.Indices.CreateAsync("ersatztv");
            return createResponse.IsValidResponse && createResponse.IsSuccess();
        }

        return true;
    }

    public async Task<Unit> Rebuild(ICachingSearchRepository searchRepository, IFallbackMetadataProvider fallbackMetadataProvider)
    {
        return Unit.Default;
    }

    public async Task<Unit> RebuildItems(
        ICachingSearchRepository searchRepository,
        IFallbackMetadataProvider fallbackMetadataProvider,
        IEnumerable<int> itemIds)
    {
        return Unit.Default;
    }

    public async Task<Unit> UpdateItems(
        ICachingSearchRepository searchRepository,
        IFallbackMetadataProvider fallbackMetadataProvider,
        List<MediaItem> items)
    {
        return Unit.Default;
    }

    public async Task<Unit> RemoveItems(IEnumerable<int> ids)
    {
        return Unit.Default;
    }

    public SearchResult Search(IClient client, string query, int skip, int limit, string searchField = "")
    {
        return new SearchResult(new List<SearchItem>(), 0);
    }

    public void Commit()
    {
    }
}
