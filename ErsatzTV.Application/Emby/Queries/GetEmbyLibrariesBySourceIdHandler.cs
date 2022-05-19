using ErsatzTV.Core.Interfaces.Repositories;
using static ErsatzTV.Application.Emby.Mapper;

namespace ErsatzTV.Application.Emby;

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
