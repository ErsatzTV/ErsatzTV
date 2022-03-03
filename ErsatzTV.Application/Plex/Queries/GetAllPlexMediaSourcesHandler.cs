using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.Plex.Mapper;

namespace ErsatzTV.Application.Plex;

public class GetAllPlexMediaSourcesHandler : IRequestHandler<GetAllPlexMediaSources, List<PlexMediaSourceViewModel>>
{
    private readonly IMediaSourceRepository _mediaSourceRepository;

    public GetAllPlexMediaSourcesHandler(IMediaSourceRepository mediaSourceRepository) =>
        _mediaSourceRepository = mediaSourceRepository;

    public Task<List<PlexMediaSourceViewModel>> Handle(
        GetAllPlexMediaSources request,
        CancellationToken cancellationToken) =>
        _mediaSourceRepository.GetAllPlex().Map(list => list.Map(ProjectToViewModel).ToList());
}