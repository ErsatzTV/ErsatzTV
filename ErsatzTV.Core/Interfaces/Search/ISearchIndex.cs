using Bugsnag;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Repositories.Caching;
using ErsatzTV.Core.Search;

namespace ErsatzTV.Core.Interfaces.Search;

public interface ISearchIndex : IDisposable
{
    Task<bool> IndexExists();
    public int Version { get; }
    Task<bool> Initialize(ILocalFileSystem localFileSystem, IConfigElementRepository configElementRepository);
    Task<Unit> Rebuild(ICachingSearchRepository searchRepository, IFallbackMetadataProvider fallbackMetadataProvider);

    Task<Unit> RebuildItems(
        ICachingSearchRepository searchRepository,
        IFallbackMetadataProvider fallbackMetadataProvider,
        IEnumerable<int> itemIds);

    Task<Unit> UpdateItems(
        ICachingSearchRepository searchRepository,
        IFallbackMetadataProvider fallbackMetadataProvider,
        List<MediaItem> items);

    Task<Unit> RemoveItems(IEnumerable<int> ids);
    Task<SearchResult> Search(IClient client, string query, int skip, int limit, string searchField = "");
    void Commit();
}
