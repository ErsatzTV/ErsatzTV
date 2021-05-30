using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Search;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Search.Queries
{
    public class
        QuerySearchIndexAllItemsHandler : IRequestHandler<QuerySearchIndexAllItems, SearchResultAllItemsViewModel>
    {
        private readonly ISearchIndex _searchIndex;

        public QuerySearchIndexAllItemsHandler(ISearchIndex searchIndex) => _searchIndex = searchIndex;

        public async Task<SearchResultAllItemsViewModel> Handle(
            QuerySearchIndexAllItems request,
            CancellationToken cancellationToken) =>
            new(
                await GetIds("movie", request.Query),
                await GetIds("show", request.Query),
                await GetIds("episode", request.Query),
                await GetIds("artist", request.Query),
                await GetIds("music_video", request.Query));

        private Task<List<int>> GetIds(string type, string query) =>
            _searchIndex.Search($"type:{type} AND ({query})", 0, 0)
                .Map(result => result.Items.Map(i => i.Id).ToList());
    }
}
