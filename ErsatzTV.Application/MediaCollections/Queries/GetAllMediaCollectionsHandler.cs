using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.MediaCollections.Mapper;

namespace ErsatzTV.Application.MediaCollections.Queries
{
    public class GetAllMediaCollectionsHandler : IRequestHandler<GetAllMediaCollections, List<MediaCollectionViewModel>>
    {
        private readonly IMediaCollectionRepository _mediaCollectionRepository;

        public GetAllMediaCollectionsHandler(IMediaCollectionRepository mediaCollectionRepository) =>
            _mediaCollectionRepository = mediaCollectionRepository;

        public Task<List<MediaCollectionViewModel>> Handle(
            GetAllMediaCollections request,
            CancellationToken cancellationToken) =>
            _mediaCollectionRepository.GetAll().Map(list => list.Map(ProjectToViewModel).ToList());
    }
}
