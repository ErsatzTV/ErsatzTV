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
            CancellationToken cancellationToken)
        {
            List<int> movieIds = await _searchIndex.Search($"type:movie AND ({request.Query})", 0, 0)
                .Map(result => result.Items.Map(i => i.Id).ToList());
            List<int> showIds = await _searchIndex.Search($"type:show AND ({request.Query})", 0, 0)
                .Map(result => result.Items.Map(i => i.Id).ToList());
            List<int> musicVideoIds = await _searchIndex.Search($"type:music_video AND ({request.Query})", 0, 0)
                .Map(result => result.Items.Map(i => i.Id).ToList());

            return new SearchResultAllItemsViewModel(movieIds, showIds, musicVideoIds);
        }
    }
}
