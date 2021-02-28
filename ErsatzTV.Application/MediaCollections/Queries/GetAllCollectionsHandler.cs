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
    public class GetAllCollectionsHandler : IRequestHandler<GetAllCollections, List<MediaCollectionViewModel>>
    {
        private readonly IMediaCollectionRepository _mediaCollectionRepository;

        public GetAllCollectionsHandler(IMediaCollectionRepository mediaCollectionRepository) =>
            _mediaCollectionRepository = mediaCollectionRepository;

        public Task<List<MediaCollectionViewModel>> Handle(
            GetAllCollections request,
            CancellationToken cancellationToken) =>
            _mediaCollectionRepository.GetAll().Map(list => list.Map(ProjectToViewModel).ToList());
    }
}
