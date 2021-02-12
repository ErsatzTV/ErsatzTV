using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.MediaItems.Mapper;

namespace ErsatzTV.Application.MediaItems.Queries
{
    public class GetAllMediaItemsHandler : IRequestHandler<GetAllMediaItems, List<MediaItemViewModel>>
    {
        private readonly IMediaItemRepository _mediaItemRepository;

        public GetAllMediaItemsHandler(IMediaItemRepository mediaItemRepository) =>
            _mediaItemRepository = mediaItemRepository;

        public Task<List<MediaItemViewModel>> Handle(
            GetAllMediaItems request,
            CancellationToken cancellationToken) =>
            _mediaItemRepository.GetAll().Map(list => list.Map(ProjectToViewModel).ToList());
    }
}
