using ErsatzTV.Application.MediaCards;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Search;
using static ErsatzTV.Application.MediaCards.Mapper;

namespace ErsatzTV.Application.Search;

public class
    QuerySearchIndexEpisodesHandler : IRequestHandler<QuerySearchIndexEpisodes,
        TelevisionEpisodeCardResultsViewModel>
{
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly ISearchIndex _searchIndex;
    private readonly ITelevisionRepository _televisionRepository;

    public QuerySearchIndexEpisodesHandler(
        ISearchIndex searchIndex,
        ITelevisionRepository televisionRepository,
        IMediaSourceRepository mediaSourceRepository)
    {
        _searchIndex = searchIndex;
        _televisionRepository = televisionRepository;
        _mediaSourceRepository = mediaSourceRepository;
    }

    public async Task<TelevisionEpisodeCardResultsViewModel> Handle(
        QuerySearchIndexEpisodes request,
        CancellationToken cancellationToken)
    {
        SearchResult searchResult = await _searchIndex.Search(
            request.Query,
            (request.PageNumber - 1) * request.PageSize,
            request.PageSize);

        Option<JellyfinMediaSource> maybeJellyfin = await _mediaSourceRepository.GetAllJellyfin()
            .Map(list => list.HeadOrNone());

        Option<EmbyMediaSource> maybeEmby = await _mediaSourceRepository.GetAllEmby()
            .Map(list => list.HeadOrNone());

        List<TelevisionEpisodeCardViewModel> items = await _televisionRepository
            .GetEpisodesForCards(searchResult.Items.Map(i => i.Id).ToList())
            .Map(list => list.Map(s => ProjectToViewModel(s, maybeJellyfin, maybeEmby, true)).ToList());

        return new TelevisionEpisodeCardResultsViewModel(searchResult.TotalCount, items, searchResult.PageMap);
    }
}