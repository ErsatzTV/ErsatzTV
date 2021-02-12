using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static LanguageExt.Prelude;
using static ErsatzTV.Application.MediaCollections.Mapper;
using static ErsatzTV.Application.MediaItems.Mapper;

namespace ErsatzTV.Application.MediaCollections.Queries
{
    public class GetSimpleMediaCollectionWithItemsByIdHandler : IRequestHandler<GetSimpleMediaCollectionWithItemsById,
        Option<Tuple<MediaCollectionViewModel, List<MediaItemSearchResultViewModel>>>>
    {
        private readonly IMediaCollectionRepository _mediaCollectionRepository;

        public GetSimpleMediaCollectionWithItemsByIdHandler(IMediaCollectionRepository mediaCollectionRepository) =>
            _mediaCollectionRepository = mediaCollectionRepository;

        public async Task<Option<Tuple<MediaCollectionViewModel, List<MediaItemSearchResultViewModel>>>> Handle(
            GetSimpleMediaCollectionWithItemsById request,
            CancellationToken cancellationToken)
        {
            Option<SimpleMediaCollection> maybeCollection =
                await _mediaCollectionRepository.GetSimpleMediaCollectionWithItems(request.Id);

            return maybeCollection.Match<Option<Tuple<MediaCollectionViewModel, List<MediaItemSearchResultViewModel>>>>(
                c => Tuple(ProjectToViewModel(c), c.Items.Map(ProjectToSearchViewModel).ToList()),
                None);
        }
    }
}
