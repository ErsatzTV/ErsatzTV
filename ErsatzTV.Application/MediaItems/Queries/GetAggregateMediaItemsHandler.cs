using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.AggregateModels;
using ErsatzTV.Core.Interfaces.Repositories;
using MediatR;

namespace ErsatzTV.Application.MediaItems.Queries
{
    public class
        GetAggregateMediaItemsHandler : IRequestHandler<GetAggregateMediaItems, AggregateMediaItemResults>
    {
        private readonly IMediaItemRepository _mediaItemRepository;

        public GetAggregateMediaItemsHandler(IMediaItemRepository mediaItemRepository) =>
            _mediaItemRepository = mediaItemRepository;

        public async Task<AggregateMediaItemResults> Handle(
            GetAggregateMediaItems request,
            CancellationToken cancellationToken)
        {
            int count = await _mediaItemRepository.GetCountByType(request.MediaType);

            IEnumerable<MediaItemSummary> allItems = await _mediaItemRepository.GetPageByType(
                request.MediaType,
                request.PageNumber,
                request.PageSize);

            var results = allItems
                .Map(
                    s => new AggregateMediaItemViewModel(
                        s.MediaItemId,
                        s.Title,
                        s.Subtitle,
                        s.SortTitle,
                        !string.IsNullOrWhiteSpace(s.PosterPath)))
                .ToList();

            return new AggregateMediaItemResults(count, results);
        }
    }
}
