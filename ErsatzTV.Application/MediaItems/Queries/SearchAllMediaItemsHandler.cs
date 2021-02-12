using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.MediaItems.Mapper;

namespace ErsatzTV.Application.MediaItems.Queries
{
    public class SearchAllMediaItemsHandler : IRequestHandler<SearchAllMediaItems, List<MediaItemSearchResultViewModel>>
    {
        private readonly IMediaItemRepository _mediaItemRepository;

        public SearchAllMediaItemsHandler(IMediaItemRepository mediaItemRepository) =>
            _mediaItemRepository = mediaItemRepository;

        public Task<List<MediaItemSearchResultViewModel>>
            Handle(SearchAllMediaItems request, CancellationToken cancellationToken) =>
            _mediaItemRepository.Search(request.SearchString).Map(list => list.Map(ProjectToSearchViewModel).ToList());
    }
}
