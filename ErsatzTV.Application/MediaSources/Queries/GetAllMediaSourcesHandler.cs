using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using MediatR;
using static ErsatzTV.Application.MediaSources.Mapper;

namespace ErsatzTV.Application.MediaSources.Queries
{
    public class GetAllMediaSourcesHandler : IRequestHandler<GetAllMediaSources, List<MediaSourceViewModel>>
    {
        private readonly IMediaSourceRepository _mediaSourceRepository;

        public GetAllMediaSourcesHandler(IMediaSourceRepository mediaSourceRepository) =>
            _mediaSourceRepository = mediaSourceRepository;

        public async Task<List<MediaSourceViewModel>> Handle(
            GetAllMediaSources request,
            CancellationToken cancellationToken) =>
            (await _mediaSourceRepository.GetAll()).Map(ProjectToViewModel).ToList();
    }
}
