using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.MediaItems.Mapper;

namespace ErsatzTV.Application.MediaItems.Queries
{
    public class GetMediaItemByIdHandler : IRequestHandler<GetMediaItemById, Option<MediaItemViewModel>>
    {
        private readonly IMediaItemRepository _mediaItemRepository;

        public GetMediaItemByIdHandler(IMediaItemRepository mediaItemRepository) =>
            _mediaItemRepository = mediaItemRepository;

        public Task<Option<MediaItemViewModel>> Handle(
            GetMediaItemById request,
            CancellationToken cancellationToken) =>
            _mediaItemRepository.Get(request.Id)
                .MapT(ProjectToViewModel);
    }
}
