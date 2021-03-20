using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Search;
using MediatR;

namespace ErsatzTV.Application.Search.Queries
{
    public class QuerySearchIndexHandler : IRequestHandler<QuerySearchIndex, SearchResult>
    {
        private readonly ISearchIndex _searchIndex;

        public QuerySearchIndexHandler(ISearchIndex searchIndex) => _searchIndex = searchIndex;

        public Task<SearchResult> Handle(QuerySearchIndex request, CancellationToken cancellationToken) =>
            _searchIndex.Search(request.Query, 0, 100);
    }
}
