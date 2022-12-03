using Bugsnag;
using ErsatzTV.Application.MediaCards;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Search;
using static ErsatzTV.Application.MediaCards.Mapper;

namespace ErsatzTV.Application.Search;

public class QuerySearchIndexSongsHandler : IRequestHandler<QuerySearchIndexSongs, SongCardResultsViewModel>
{
    private readonly IClient _client;
    private readonly ISearchIndex _searchIndex;
    private readonly ISongRepository _songRepository;

    public QuerySearchIndexSongsHandler(IClient client, ISearchIndex searchIndex, ISongRepository songRepository)
    {
        _client = client;
        _searchIndex = searchIndex;
        _songRepository = songRepository;
    }

    public async Task<SongCardResultsViewModel> Handle(
        QuerySearchIndexSongs request,
        CancellationToken cancellationToken)
    {
        SearchResult searchResult = _searchIndex.Search(
            _client,
            request.Query,
            (request.PageNumber - 1) * request.PageSize,
            request.PageSize);

        List<SongCardViewModel> items = await _songRepository
            .GetSongsForCards(searchResult.Items.Map(i => i.Id).ToList())
            .Map(list => list.Map(ProjectToViewModel).ToList());

        return new SongCardResultsViewModel(searchResult.TotalCount, items, searchResult.PageMap);
    }
}
