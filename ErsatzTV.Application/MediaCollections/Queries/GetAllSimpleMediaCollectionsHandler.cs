using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using MediatR;
using static ErsatzTV.Application.MediaCollections.Mapper;

namespace ErsatzTV.Application.MediaCollections.Queries
{
    public class
        GetAllSimpleMediaCollectionsHandler : IRequestHandler<GetAllSimpleMediaCollections,
            List<MediaCollectionViewModel>>
    {
        private readonly IMediaCollectionRepository _mediaCollectionRepository;

        public GetAllSimpleMediaCollectionsHandler(IMediaCollectionRepository mediaCollectionRepository) =>
            _mediaCollectionRepository = mediaCollectionRepository;

        public async Task<List<MediaCollectionViewModel>> Handle(
            GetAllSimpleMediaCollections request,
            CancellationToken cancellationToken) =>
            (await _mediaCollectionRepository.GetSimpleMediaCollections()).Map(ProjectToViewModel).ToList();
    }
}
