using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.Search.Commands
{
    public class RefreshSearchIndexHandler : MediatR.IRequestHandler<RefreshSearchIndex, Unit>
    {
        private readonly ILogger<RefreshSearchIndexHandler> _logger;
        private readonly ISearchRepository _searchRepository;
        private readonly ISearchIndex _searchIndex;

        public RefreshSearchIndexHandler(
            ISearchIndex searchIndex,
            ILogger<RefreshSearchIndexHandler> logger,
            ISearchRepository searchRepository)
        {
            _searchIndex = searchIndex;
            _logger = logger;
            _searchRepository = searchRepository;
        }

        public async Task<Unit> Handle(RefreshSearchIndex request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Refreshing search index");

            List<MediaItem> items = await _searchRepository.GetItemsToIndex();
            await _searchIndex.AddItems(items);

            _logger.LogDebug("Done refreshing search index");

            return Unit.Default;
        }
    }
}
