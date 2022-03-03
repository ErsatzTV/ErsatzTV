using ErsatzTV.Core.Interfaces.Repositories;
using static ErsatzTV.Application.Plex.Mapper;

namespace ErsatzTV.Application.Plex;

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