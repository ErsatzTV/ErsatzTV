using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.Emby.Mapper;

namespace ErsatzTV.Application.Emby;

public class
    GetEmbyMediaSourceByIdHandler : IRequestHandler<GetEmbyMediaSourceById, Option<EmbyMediaSourceViewModel>>
{
    private readonly IMediaSourceRepository _mediaSourceRepository;

    public GetEmbyMediaSourceByIdHandler(IMediaSourceRepository mediaSourceRepository) =>
        _mediaSourceRepository = mediaSourceRepository;

    public Task<Option<EmbyMediaSourceViewModel>> Handle(
        GetEmbyMediaSourceById request,
        CancellationToken cancellationToken) =>
        _mediaSourceRepository.GetEmby(request.EmbyMediaSourceId).MapT(ProjectToViewModel);
}