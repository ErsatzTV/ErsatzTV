using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.Jellyfin.Mapper;

namespace ErsatzTV.Application.Jellyfin;

public class GetJellyfinPathReplacementsBySourceIdHandler : IRequestHandler<GetJellyfinPathReplacementsBySourceId,
    List<JellyfinPathReplacementViewModel>>
{
    private readonly IMediaSourceRepository _mediaSourceRepository;

    public GetJellyfinPathReplacementsBySourceIdHandler(IMediaSourceRepository mediaSourceRepository) =>
        _mediaSourceRepository = mediaSourceRepository;

    public Task<List<JellyfinPathReplacementViewModel>> Handle(
        GetJellyfinPathReplacementsBySourceId request,
        CancellationToken cancellationToken) =>
        _mediaSourceRepository.GetJellyfinPathReplacements(request.JellyfinMediaSourceId)
            .Map(list => list.Map(ProjectToViewModel).ToList());
}