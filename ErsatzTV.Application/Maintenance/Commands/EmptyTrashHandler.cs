using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Search;
using ErsatzTV.Infrastructure.Search;

namespace ErsatzTV.Application.Maintenance;

public class EmptyTrashHandler : IRequestHandler<EmptyTrash, Either<BaseError, Unit>>
{
    private readonly IMediaItemRepository _mediaItemRepository;
    private readonly ISearchIndex _searchIndex;

    public EmptyTrashHandler(
        IMediaItemRepository mediaItemRepository,
        ISearchIndex searchIndex)
    {
        _mediaItemRepository = mediaItemRepository;
        _searchIndex = searchIndex;
    }

    public async Task<Either<BaseError, Unit>> Handle(
        EmptyTrash request,
        CancellationToken cancellationToken)
    {
        string[] types =
        {
            SearchIndex.MovieType,
            SearchIndex.ShowType,
            SearchIndex.SeasonType,
            SearchIndex.EpisodeType,
            SearchIndex.MusicVideoType,
            SearchIndex.OtherVideoType,
            SearchIndex.SongType,
            SearchIndex.ArtistType
        };

        var ids = new List<int>();

        foreach (string type in types)
        {
            SearchResult result = await _searchIndex.Search($"type:{type} AND (state:FileNotFound)", 0, 0);
            ids.AddRange(result.Items.Map(i => i.Id));
        }

        Either<BaseError, Unit> deleteResult = await _mediaItemRepository.DeleteItems(ids);
        if (deleteResult.IsRight)
        {
            await _searchIndex.RemoveItems(ids);
            _searchIndex.Commit();
        }

        return deleteResult;
    }
}
