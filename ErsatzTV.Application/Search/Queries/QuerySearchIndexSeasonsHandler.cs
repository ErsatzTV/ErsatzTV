using ErsatzTV.Application.MediaCards;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Search;
using static ErsatzTV.Application.MediaCards.Mapper;

namespace ErsatzTV.Application.Search;

public class
    QuerySearchIndexSeasonsHandler : IRequestHandler<QuerySearchIndexSeasons, TelevisionSeasonCardResultsViewModel>
{
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly ISearchIndex _searchIndex;
    private readonly ITelevisionRepository _televisionRepository;

    public QuerySearchIndexSeasonsHandler(
        ISearchIndex searchIndex,
        ITelevisionRepository televisionRepository,
        IMediaSourceRepository mediaSourceRepository)
    {
        _searchIndex = searchIndex;
        _televisionRepository = televisionRepository;
        _mediaSourceRepository = mediaSourceRepository;
    }

    public async Task<TelevisionSeasonCardResultsViewModel> Handle(
        QuerySearchIndexSeasons request,
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

        List<TelevisionSeasonCardViewModel> items = await _televisionRepository
            .GetSeasonsForCards(searchResult.Items.Map(i => i.Id).ToList())
            .Map(list => list.Map(s => ProjectToViewModel(s, maybeJellyfin, maybeEmby)).ToList());

        return new TelevisionSeasonCardResultsViewModel(searchResult.TotalCount, items, searchResult.PageMap);
    }
}