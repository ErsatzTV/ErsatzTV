using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.Emby.Mapper;

namespace ErsatzTV.Application.Emby;

public class GetAllEmbyMediaSourcesHandler : IRequestHandler<GetAllEmbyMediaSources, List<EmbyMediaSourceViewModel>>
{
    private readonly IMediaSourceRepository _mediaSourceRepository;

    public GetAllEmbyMediaSourcesHandler(IMediaSourceRepository mediaSourceRepository) =>
        _mediaSourceRepository = mediaSourceRepository;

    public Task<List<EmbyMediaSourceViewModel>> Handle(
        GetAllEmbyMediaSources request,
        CancellationToken cancellationToken) =>
        _mediaSourceRepository.GetAllEmby().Map(list => list.Map(ProjectToViewModel).ToList());
}