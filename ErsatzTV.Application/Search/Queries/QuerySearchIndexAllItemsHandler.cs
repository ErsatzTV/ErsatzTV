﻿using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Infrastructure.Search;

namespace ErsatzTV.Application.Search;

public class
    QuerySearchIndexAllItemsHandler : IRequestHandler<QuerySearchIndexAllItems, SearchResultAllItemsViewModel>
{
    private readonly ISearchIndex _searchIndex;

    public QuerySearchIndexAllItemsHandler(ISearchIndex searchIndex) => _searchIndex = searchIndex;

    public async Task<SearchResultAllItemsViewModel> Handle(
        QuerySearchIndexAllItems request,
        CancellationToken cancellationToken) =>
        new(
            await GetIds(SearchIndex.MovieType, request.Query),
            await GetIds(SearchIndex.ShowType, request.Query),
            await GetIds(SearchIndex.SeasonType, request.Query),
            await GetIds(SearchIndex.EpisodeType, request.Query),
            await GetIds(SearchIndex.ArtistType, request.Query),
            await GetIds(SearchIndex.MusicVideoType, request.Query),
            await GetIds(SearchIndex.OtherVideoType, request.Query),
            await GetIds(SearchIndex.SongType, request.Query));

    private Task<List<int>> GetIds(string type, string query) =>
        _searchIndex.Search($"type:{type} AND ({query})", 0, 0)
            .Map(result => result.Items.Map(i => i.Id).ToList());
}