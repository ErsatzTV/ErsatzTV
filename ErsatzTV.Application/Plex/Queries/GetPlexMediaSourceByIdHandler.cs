using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.Plex.Mapper;

namespace ErsatzTV.Application.Plex.Queries
{
    public class
        GetPlexMediaSourceByIdHandler : IRequestHandler<GetPlexMediaSourceById, Option<PlexMediaSourceViewModel>>
    {
        private readonly IMediaSourceRepository _mediaSourceRepository;

        public GetPlexMediaSourceByIdHandler(IMediaSourceRepository mediaSourceRepository) =>
            _mediaSourceRepository = mediaSourceRepository;

        public Task<Option<PlexMediaSourceViewModel>> Handle(
            GetPlexMediaSourceById request,
            CancellationToken cancellationToken) =>
            _mediaSourceRepository.GetPlex(request.PlexMediaSourceId).MapT(ProjectToViewModel);
    }
}
