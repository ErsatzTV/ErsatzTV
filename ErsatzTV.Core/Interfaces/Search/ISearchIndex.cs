using Bugsnag;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Search;

namespace ErsatzTV.Core.Interfaces.Search;

public interface ISearchIndex : IDisposable
{
    int Version { get; }
    Task<bool> IndexExists();

    Task<bool> Initialize(
        ILocalFileSystem localFileSystem,
        IConfigElementRepository configElementRepository,
        CancellationToken cancellationToken);

    Task<Unit> Rebuild(
        ISearchRepository searchRepository,
        IFallbackMetadataProvider fallbackMetadataProvider,
        ILanguageCodeService languageCodeService,
        CancellationToken cancellationToken);

    Task<Unit> RebuildItems(
        ISearchRepository searchRepository,
        IFallbackMetadataProvider fallbackMetadataProvider,
        ILanguageCodeService languageCodeService,
        IEnumerable<int> itemIds,
        CancellationToken cancellationToken);

    Task<Unit> UpdateItems(
        ISearchRepository searchRepository,
        IFallbackMetadataProvider fallbackMetadataProvider,
        ILanguageCodeService languageCodeService,
        List<MediaItem> items);

    Task<bool> RemoveItems(IEnumerable<int> ids);

    Task<SearchResult> Search(
        IClient client,
        string query,
        string smartCollectionName,
        int skip,
        int limit,
        CancellationToken cancellationToken);

    void Commit();
}
