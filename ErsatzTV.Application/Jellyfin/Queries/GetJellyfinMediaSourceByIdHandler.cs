using ErsatzTV.Core.Interfaces.Repositories;
using static ErsatzTV.Application.Jellyfin.Mapper;

namespace ErsatzTV.Application.Jellyfin;

public class
    GetJellyfinMediaSourceByIdHandler : IRequestHandler<GetJellyfinMediaSourceById,
        Option<JellyfinMediaSourceViewModel>>
{
    private readonly IMediaSourceRepository _mediaSourceRepository;

    public GetJellyfinMediaSourceByIdHandler(IMediaSourceRepository mediaSourceRepository) =>
        _mediaSourceRepository = mediaSourceRepository;

    public Task<Option<JellyfinMediaSourceViewModel>> Handle(
        GetJellyfinMediaSourceById request,
        CancellationToken cancellationToken) =>
        _mediaSourceRepository.GetJellyfin(request.JellyfinMediaSourceId).MapT(ProjectToViewModel);
}
