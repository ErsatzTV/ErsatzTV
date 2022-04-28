using ErsatzTV.Core.Interfaces.Repositories;
using static ErsatzTV.Application.Jellyfin.Mapper;

namespace ErsatzTV.Application.Jellyfin;

public class
    GetAllJellyfinMediaSourcesHandler : IRequestHandler<GetAllJellyfinMediaSources,
        List<JellyfinMediaSourceViewModel>>
{
    private readonly IMediaSourceRepository _mediaSourceRepository;

    public GetAllJellyfinMediaSourcesHandler(IMediaSourceRepository mediaSourceRepository) =>
        _mediaSourceRepository = mediaSourceRepository;

    public Task<List<JellyfinMediaSourceViewModel>> Handle(
        GetAllJellyfinMediaSources request,
        CancellationToken cancellationToken) =>
        _mediaSourceRepository.GetAllJellyfin().Map(list => list.Map(ProjectToViewModel).ToList());
}
