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
    public class GetCollectionItemsHandler : IRequestHandler<GetCollectionItems,
        Option<IEnumerable<MediaItemViewModel>>>
    {
        private readonly IMediaCollectionRepository _mediaCollectionRepository;

        public GetCollectionItemsHandler(IMediaCollectionRepository mediaCollectionRepository) =>
            _mediaCollectionRepository = mediaCollectionRepository;

        public Task<Option<IEnumerable<MediaItemViewModel>>> Handle(
            GetCollectionItems request,
            CancellationToken cancellationToken) =>
            _mediaCollectionRepository.GetItems(request.Id)
                .MapT(mediaItems => mediaItems.Map(ProjectToViewModel));
    }
}
