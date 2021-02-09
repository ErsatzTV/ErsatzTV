using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.MediaCollections.Mapper;

namespace ErsatzTV.Application.MediaCollections.Queries
{
    public class
        GetSimpleMediaCollectionByIdHandler : IRequestHandler<GetSimpleMediaCollectionById,
            Option<MediaCollectionViewModel>>
    {
        private readonly IMediaCollectionRepository _mediaCollectionRepository;

        public GetSimpleMediaCollectionByIdHandler(IMediaCollectionRepository mediaCollectionRepository) =>
            _mediaCollectionRepository = mediaCollectionRepository;

        public Task<Option<MediaCollectionViewModel>> Handle(
            GetSimpleMediaCollectionById request,
            CancellationToken cancellationToken) =>
            _mediaCollectionRepository.GetSimpleMediaCollection(request.Id)
                .MapT(ProjectToViewModel);
    }
}
