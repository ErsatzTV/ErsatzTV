using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.Emby.Mapper;

namespace ErsatzTV.Application.Emby;

public class GetEmbyPathReplacementsBySourceIdHandler : IRequestHandler<GetEmbyPathReplacementsBySourceId,
    List<EmbyPathReplacementViewModel>>
{
    private readonly IMediaSourceRepository _mediaSourceRepository;

    public GetEmbyPathReplacementsBySourceIdHandler(IMediaSourceRepository mediaSourceRepository) =>
        _mediaSourceRepository = mediaSourceRepository;

    public Task<List<EmbyPathReplacementViewModel>> Handle(
        GetEmbyPathReplacementsBySourceId request,
        CancellationToken cancellationToken) =>
        _mediaSourceRepository.GetEmbyPathReplacements(request.EmbyMediaSourceId)
            .Map(list => list.Map(ProjectToViewModel).ToList());
}