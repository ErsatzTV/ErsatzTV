using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Application.MediaCards;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Search;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.MediaCards.Mapper;

namespace ErsatzTV.Application.Search.Queries
{
    public class
        QuerySearchIndexShowsHandler : IRequestHandler<QuerySearchIndexShows, TelevisionShowCardResultsViewModel>
    {
        private readonly ISearchIndex _searchIndex;
        private readonly ITelevisionRepository _televisionRepository;

        public QuerySearchIndexShowsHandler(ISearchIndex searchIndex, ITelevisionRepository televisionRepository)
        {
            _searchIndex = searchIndex;
            _televisionRepository = televisionRepository;
        }

        public async Task<TelevisionShowCardResultsViewModel> Handle(
            QuerySearchIndexShows request,
            CancellationToken cancellationToken)
        {
            (List<SearchItem> searchItems, int totalCount) =
                await _searchIndex.Search(
                    request.Query,
                    (request.PageNumber - 1) * request.PageSize,
                    request.PageSize);

            List<TelevisionShowCardViewModel> items = await _televisionRepository
                .GetShowsForCards(searchItems.Map(i => i.Id).ToList())
                .Map(list => list.Map(ProjectToViewModel).ToList());

            return new TelevisionShowCardResultsViewModel(totalCount, items);
        }
    }
}
