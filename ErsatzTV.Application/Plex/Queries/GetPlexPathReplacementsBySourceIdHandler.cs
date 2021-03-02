using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.Plex.Mapper;

namespace ErsatzTV.Application.Plex.Queries
{
    public class GetPlexPathReplacementsBySourceIdHandler : IRequestHandler<GetPlexPathReplacementsBySourceId,
        List<PlexPathReplacementViewModel>>
    {
        private readonly IMediaSourceRepository _mediaSourceRepository;

        public GetPlexPathReplacementsBySourceIdHandler(IMediaSourceRepository mediaSourceRepository) =>
            _mediaSourceRepository = mediaSourceRepository;

        public Task<List<PlexPathReplacementViewModel>> Handle(
            GetPlexPathReplacementsBySourceId request,
            CancellationToken cancellationToken) =>
            _mediaSourceRepository.GetPlexPathReplacements(request.PlexMediaSourceId)
                .Map(list => list.Map(ProjectToViewModel).ToList());
    }
}
