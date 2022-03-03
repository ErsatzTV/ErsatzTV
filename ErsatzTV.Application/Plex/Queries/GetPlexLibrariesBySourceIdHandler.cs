using ErsatzTV.Core.Interfaces.Repositories;
using static ErsatzTV.Application.Plex.Mapper;

namespace ErsatzTV.Application.Plex;

public class
    GetPlexLibrariesBySourceIdHandler : IRequestHandler<GetPlexLibrariesBySourceId,
        List<PlexLibraryViewModel>>
{
    private readonly IMediaSourceRepository _mediaSourceRepository;

    public GetPlexLibrariesBySourceIdHandler(IMediaSourceRepository mediaSourceRepository) =>
        _mediaSourceRepository = mediaSourceRepository;

    public Task<List<PlexLibraryViewModel>> Handle(
        GetPlexLibrariesBySourceId request,
        CancellationToken cancellationToken) =>
        _mediaSourceRepository.GetPlexLibraries(request.PlexMediaSourceId)
            .Map(list => list.Map(ProjectToViewModel).ToList());
}