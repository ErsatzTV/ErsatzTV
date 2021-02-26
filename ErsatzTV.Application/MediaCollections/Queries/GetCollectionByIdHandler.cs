using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.MediaCollections.Mapper;

namespace ErsatzTV.Application.MediaCollections.Queries
{
    public class
        GetCollectionByIdHandler : IRequestHandler<GetCollectionById,
            Option<MediaCollectionViewModel>>
    {
        private readonly IMediaCollectionRepository _mediaCollectionRepository;

        public GetCollectionByIdHandler(IMediaCollectionRepository mediaCollectionRepository) =>
            _mediaCollectionRepository = mediaCollectionRepository;

        public Task<Option<MediaCollectionViewModel>> Handle(
            GetCollectionById request,
            CancellationToken cancellationToken) =>
            _mediaCollectionRepository.Get(request.Id)
                .MapT(ProjectToViewModel);
    }
}
