using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;

namespace ErsatzTV.Application.Search;

public class ReindexMediaItemsHandler(
    ISearchRepository searchRepository,
    IFallbackMetadataProvider fallbackMetadataProvider,
    ILanguageCodeService languageCodeService,
    ISearchIndex searchIndex)
    : IRequestHandler<ReindexMediaItems>
{
    public async Task Handle(ReindexMediaItems request, CancellationToken cancellationToken)
    {
        await searchIndex.RebuildItems(
            searchRepository,
            fallbackMetadataProvider,
            languageCodeService,
            request.MediaItemIds,
            cancellationToken);

        searchIndex.Commit();
    }
}
