using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Search;
using LanguageExt;

namespace ErsatzTV.Application.Search.Commands
{
    public class RemoveItemsFromSearchIndexHandler : MediatR.IRequestHandler<RemoveItemsFromSearchIndex, Unit>
    {
        private readonly ISearchIndex _searchIndex;

        public RemoveItemsFromSearchIndexHandler(ISearchIndex searchIndex) => _searchIndex = searchIndex;

        public Task<Unit> Handle(RemoveItemsFromSearchIndex request, CancellationToken cancellationToken) =>
            _searchIndex.RemoveItems(request.MediaItemIds);
    }
}
