using Bugsnag;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Infrastructure.Search;

namespace ErsatzTV.Application.Search;

public class QuerySearchIndexAllItemsHandler(IClient client, ISearchIndex searchIndex)
    : IRequestHandler<QuerySearchIndexAllItems, SearchResultAllItemsViewModel>
{
    public async Task<SearchResultAllItemsViewModel> Handle(
        QuerySearchIndexAllItems request,
        CancellationToken cancellationToken) =>
        new(
            await GetIds(LuceneSearchIndex.MovieType, request.Query),
            await GetIds(LuceneSearchIndex.ShowType, request.Query),
            await GetIds(LuceneSearchIndex.SeasonType, request.Query),
            await GetIds(LuceneSearchIndex.EpisodeType, request.Query),
            await GetIds(LuceneSearchIndex.ArtistType, request.Query),
            await GetIds(LuceneSearchIndex.MusicVideoType, request.Query),
            await GetIds(LuceneSearchIndex.OtherVideoType, request.Query),
            await GetIds(LuceneSearchIndex.SongType, request.Query),
            await GetIds(LuceneSearchIndex.ImageType, request.Query),
            await GetIds(LuceneSearchIndex.RemoteStreamType, request.Query));

    private async Task<List<int>> GetIds(string type, string query) =>
        (await searchIndex.Search(client, $"type:{type} AND ({query})", string.Empty, 0, 0)).Items.Map(i => i.Id)
        .ToList();
}
