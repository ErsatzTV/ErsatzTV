using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Search;
using LanguageExt;

namespace ErsatzTV.Application.Search.Commands
{
    public class AddItemsToSearchIndexHandler : MediatR.IRequestHandler<AddItemsToSearchIndex, Unit>
    {
        private readonly ISearchIndex _searchIndex;

        public AddItemsToSearchIndexHandler(ISearchIndex searchIndex) => _searchIndex = searchIndex;

        public Task<Unit> Handle(AddItemsToSearchIndex request, CancellationToken cancellationToken) =>
            _searchIndex.AddItems(request.MediaItems);
    }
}
