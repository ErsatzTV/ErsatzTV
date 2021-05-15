using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.Jellyfin.Mapper;

namespace ErsatzTV.Application.Jellyfin.Queries
{
    public class
        GetJellyfinLibrariesBySourceIdHandler : IRequestHandler<GetJellyfinLibrariesBySourceId,
            List<JellyfinLibraryViewModel>>
    {
        private readonly IMediaSourceRepository _mediaSourceRepository;

        public GetJellyfinLibrariesBySourceIdHandler(IMediaSourceRepository mediaSourceRepository) =>
            _mediaSourceRepository = mediaSourceRepository;

        public Task<List<JellyfinLibraryViewModel>> Handle(
            GetJellyfinLibrariesBySourceId request,
            CancellationToken cancellationToken) =>
            _mediaSourceRepository.GetJellyfinLibraries(request.JellyfinMediaSourceId)
                .Map(list => list.Map(ProjectToViewModel).ToList());
    }
}
