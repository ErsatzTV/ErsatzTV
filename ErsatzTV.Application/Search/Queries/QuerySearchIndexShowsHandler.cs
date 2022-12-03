using Bugsnag;
using ErsatzTV.Application.MediaCards;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Search;
using static ErsatzTV.Application.MediaCards.Mapper;

namespace ErsatzTV.Application.Search;

public class
    QuerySearchIndexShowsHandler : IRequestHandler<QuerySearchIndexShows, TelevisionShowCardResultsViewModel>
{
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IClient _client;
    private readonly ISearchIndex _searchIndex;
    private readonly ITelevisionRepository _televisionRepository;

    public QuerySearchIndexShowsHandler(
        IClient client,
        ISearchIndex searchIndex,
        ITelevisionRepository televisionRepository,
        IMediaSourceRepository mediaSourceRepository)
    {
        _client = client;
        _searchIndex = searchIndex;
        _televisionRepository = televisionRepository;
        _mediaSourceRepository = mediaSourceRepository;
    }

    public async Task<TelevisionShowCardResultsViewModel> Handle(
        QuerySearchIndexShows request,
        CancellationToken cancellationToken)
    {
        SearchResult searchResult = _searchIndex.Search(
            _client,
            request.Query,
            (request.PageNumber - 1) * request.PageSize,
            request.PageSize);

        Option<JellyfinMediaSource> maybeJellyfin = await _mediaSourceRepository.GetAllJellyfin()
            .Map(list => list.HeadOrNone());

        Option<EmbyMediaSource> maybeEmby = await _mediaSourceRepository.GetAllEmby()
            .Map(list => list.HeadOrNone());

        List<TelevisionShowCardViewModel> items = await _televisionRepository
            .GetShowsForCards(searchResult.Items.Map(i => i.Id).ToList())
            .Map(list => list.Map(s => ProjectToViewModel(s, maybeJellyfin, maybeEmby)).ToList());

        return new TelevisionShowCardResultsViewModel(searchResult.TotalCount, items, searchResult.PageMap);
    }
}
