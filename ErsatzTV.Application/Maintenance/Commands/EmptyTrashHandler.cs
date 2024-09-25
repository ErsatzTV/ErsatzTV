using Bugsnag;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Search;

namespace ErsatzTV.Application.Maintenance;

public class EmptyTrashHandler : IRequestHandler<EmptyTrash, Either<BaseError, Unit>>
{
    private readonly IClient _client;
    private readonly IMediaItemRepository _mediaItemRepository;
    private readonly ISearchIndex _searchIndex;

    public EmptyTrashHandler(
        IClient client,
        IMediaItemRepository mediaItemRepository,
        ISearchIndex searchIndex)
    {
        _client = client;
        _mediaItemRepository = mediaItemRepository;
        _searchIndex = searchIndex;
    }

    public async Task<Either<BaseError, Unit>> Handle(
        EmptyTrash request,
        CancellationToken cancellationToken)
    {
        SearchResult result = await _searchIndex.Search(_client, "state:FileNotFound", 0, 10_000);
        var ids = result.Items.Map(i => i.Id).ToList();

        // ElasticSearch remove items may fail, so do that first
        if (await _searchIndex.RemoveItems(ids))
        {
            _searchIndex.Commit();
            return await _mediaItemRepository.DeleteItems(ids);
        }

        return BaseError.New("Failed to empty trash");
    }
}
