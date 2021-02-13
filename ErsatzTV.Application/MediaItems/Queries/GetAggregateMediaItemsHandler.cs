using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using MediatR;

namespace ErsatzTV.Application.MediaItems.Queries
{
    public class
        GetAggregateMediaItemsHandler : IRequestHandler<GetAggregateMediaItems, List<AggregateMediaItemViewModel>>
    {
        private readonly IMediaItemRepository _mediaItemRepository;

        public GetAggregateMediaItemsHandler(IMediaItemRepository mediaItemRepository) =>
            _mediaItemRepository = mediaItemRepository;

        public async Task<List<AggregateMediaItemViewModel>> Handle(
            GetAggregateMediaItems request,
            CancellationToken cancellationToken)
        {
            IEnumerable<MediaItem> allItems = await _mediaItemRepository.GetAll(request.MediaType);

            if (!string.IsNullOrEmpty(request.SearchString))
            {
                allItems = allItems.Filter(
                    i => i.Metadata?.Title.Contains(request.SearchString, StringComparison.OrdinalIgnoreCase) ==
                         true);
            }

            return allItems.GroupBy(c => new { c.Source.Name, c.Metadata.Title }).Map(
                    group => new AggregateMediaItemViewModel(
                        group.Key.Name,
                        group.Key.Title,
                        request.MediaType == MediaType.TvShow
                            ? $"{group.Count()} Episodes"
                            : group.Min(i => i.Metadata?.Aired?.Year).ToString(),
                        group.Count(),
                        group.Count() == 1 ? DisplayDuration(group.Head()) : string.Empty))
                .ToList();
        }

        private static string DisplayDuration(MediaItem mediaItem) => string.Format(
            mediaItem.Metadata?.Duration.TotalHours >= 1 ? @"{0:h\:mm\:ss}" : @"{0:mm\:ss}",
            mediaItem.Metadata?.Duration);
    }
}
