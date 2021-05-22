using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.Emby.Mapper;

namespace ErsatzTV.Application.Emby.Queries
{
    public class
        GetEmbyLibrariesBySourceIdHandler : IRequestHandler<GetEmbyLibrariesBySourceId, List<EmbyLibraryViewModel>>
    {
        private readonly IMediaSourceRepository _mediaSourceRepository;

        public GetEmbyLibrariesBySourceIdHandler(IMediaSourceRepository mediaSourceRepository) =>
            _mediaSourceRepository = mediaSourceRepository;

        public Task<List<EmbyLibraryViewModel>> Handle(
            GetEmbyLibrariesBySourceId request,
            CancellationToken cancellationToken) =>
            _mediaSourceRepository.GetEmbyLibraries(request.EmbyMediaSourceId)
                .Map(list => list.Map(ProjectToViewModel).ToList());
    }
}
