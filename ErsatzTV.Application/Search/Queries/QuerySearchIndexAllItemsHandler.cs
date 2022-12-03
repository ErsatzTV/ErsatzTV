using Bugsnag;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Infrastructure.Search;

namespace ErsatzTV.Application.Search;

public class QuerySearchIndexAllItemsHandler : IRequestHandler<QuerySearchIndexAllItems, SearchResultAllItemsViewModel>
{
    private readonly IClient _client;
    private readonly ISearchIndex _searchIndex;

    public QuerySearchIndexAllItemsHandler(IClient client, ISearchIndex searchIndex)
    {
        _client = client;
        _searchIndex = searchIndex;
    }

    public Task<SearchResultAllItemsViewModel> Handle(
        QuerySearchIndexAllItems request,
        CancellationToken cancellationToken) =>
        new SearchResultAllItemsViewModel(
            GetIds(SearchIndex.MovieType, request.Query),
            GetIds(SearchIndex.ShowType, request.Query),
            GetIds(SearchIndex.SeasonType, request.Query),
            GetIds(SearchIndex.EpisodeType, request.Query),
            GetIds(SearchIndex.ArtistType, request.Query),
            GetIds(SearchIndex.MusicVideoType, request.Query),
            GetIds(SearchIndex.OtherVideoType, request.Query),
            GetIds(SearchIndex.SongType, request.Query)).AsTask();

    private List<int> GetIds(string type, string query) =>
        _searchIndex.Search(_client, $"type:{type} AND ({query})", 0, 0).Items.Map(i => i.Id).ToList();
}
