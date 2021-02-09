using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.MediaItems.Mapper;

namespace ErsatzTV.Application.MediaCollections.Queries
{
    public class GetSimpleMediaCollectionItemsHandler : IRequestHandler<GetSimpleMediaCollectionItems,
        Option<IEnumerable<MediaItemViewModel>>>
    {
        private readonly IMediaCollectionRepository _mediaCollectionRepository;

        public GetSimpleMediaCollectionItemsHandler(IMediaCollectionRepository mediaCollectionRepository) =>
            _mediaCollectionRepository = mediaCollectionRepository;

        public Task<Option<IEnumerable<MediaItemViewModel>>> Handle(
            GetSimpleMediaCollectionItems request,
            CancellationToken cancellationToken) =>
            _mediaCollectionRepository.GetSimpleMediaCollectionItems(request.Id)
                .MapT(mediaItems => mediaItems.Map(ProjectToViewModel));
    }
}
