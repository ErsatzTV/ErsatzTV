using ErsatzTV.Core.Interfaces.Search;

namespace ErsatzTV.Application.Search;

public class RemoveMediaItemsHandler : IRequestHandler<ReindexMediaItems, Unit>
{
    private readonly ISearchIndex _searchIndex;

    public RemoveMediaItemsHandler(ISearchIndex searchIndex) => _searchIndex = searchIndex;

    public async Task<Unit> Handle(ReindexMediaItems request, CancellationToken cancellationToken)
    {
        await _searchIndex.RemoveItems(request.MediaItemIds);
        _searchIndex.Commit();
        return Unit.Default;
    }
}
