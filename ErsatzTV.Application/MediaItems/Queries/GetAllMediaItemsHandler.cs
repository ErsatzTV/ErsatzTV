using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using MediatR;
using static ErsatzTV.Application.MediaItems.Mapper;

namespace ErsatzTV.Application.MediaItems.Queries
{
    public class GetAllMediaItemsHandler : IRequestHandler<GetAllMediaItems, List<MediaItemViewModel>>
    {
        private readonly IMediaItemRepository _mediaItemRepository;

        public GetAllMediaItemsHandler(IMediaItemRepository mediaItemRepository) =>
            _mediaItemRepository = mediaItemRepository;

        public async Task<List<MediaItemViewModel>> Handle(
            GetAllMediaItems request,
            CancellationToken cancellationToken) =>
            (await _mediaItemRepository.GetAll()).Map(ProjectToViewModel).ToList();
    }
}
