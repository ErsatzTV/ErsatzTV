using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories.Caching;
using ErsatzTV.Core.Interfaces.Search;

namespace ErsatzTV.Application.Search;

public class ReindexMediaItemsHandler : IRequestHandler<ReindexMediaItems, Unit>
{
    private readonly ICachingSearchRepository _cachingSearchRepository;
    private readonly IFallbackMetadataProvider _fallbackMetadataProvider;
    private readonly ISearchIndex _searchIndex;

    public ReindexMediaItemsHandler(
        ICachingSearchRepository cachingSearchRepository,
        IFallbackMetadataProvider fallbackMetadataProvider,
        ISearchIndex searchIndex)
    {
        _cachingSearchRepository = cachingSearchRepository;
        _fallbackMetadataProvider = fallbackMetadataProvider;
        _searchIndex = searchIndex;
    }

    public async Task<Unit> Handle(ReindexMediaItems request, CancellationToken cancellationToken)
    {
        await _searchIndex.RebuildItems(_cachingSearchRepository, _fallbackMetadataProvider, request.MediaItemIds);
        _searchIndex.Commit();
        return Unit.Default;
    }
}
