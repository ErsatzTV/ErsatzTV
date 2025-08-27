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
            await GetIds(LuceneSearchIndex.MovieType, request.Query, cancellationToken),
            await GetIds(LuceneSearchIndex.ShowType, request.Query, cancellationToken),
            await GetIds(LuceneSearchIndex.SeasonType, request.Query, cancellationToken),
            await GetIds(LuceneSearchIndex.EpisodeType, request.Query, cancellationToken),
            await GetIds(LuceneSearchIndex.ArtistType, request.Query, cancellationToken),
            await GetIds(LuceneSearchIndex.MusicVideoType, request.Query, cancellationToken),
            await GetIds(LuceneSearchIndex.OtherVideoType, request.Query, cancellationToken),
            await GetIds(LuceneSearchIndex.SongType, request.Query, cancellationToken),
            await GetIds(LuceneSearchIndex.ImageType, request.Query, cancellationToken),
            await GetIds(LuceneSearchIndex.RemoteStreamType, request.Query, cancellationToken));

    private async Task<List<int>> GetIds(string type, string query, CancellationToken cancellationToken) =>
        (await searchIndex.Search(client, $"type:{type} AND ({query})", string.Empty, 0, 0, cancellationToken)).Items
        .Map(i => i.Id)
        .ToList();
}
