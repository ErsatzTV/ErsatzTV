using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.Television.Mapper;

namespace ErsatzTV.Application.Television;

public class
    GetTelevisionSeasonByIdHandler : IRequestHandler<GetTelevisionSeasonById, Option<TelevisionSeasonViewModel>>
{
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly ITelevisionRepository _televisionRepository;

    public GetTelevisionSeasonByIdHandler(
        ITelevisionRepository televisionRepository,
        IMediaSourceRepository mediaSourceRepository)
    {
        _televisionRepository = televisionRepository;
        _mediaSourceRepository = mediaSourceRepository;
    }

    public async Task<Option<TelevisionSeasonViewModel>> Handle(
        GetTelevisionSeasonById request,
        CancellationToken cancellationToken)
    {
        Option<JellyfinMediaSource> maybeJellyfin = await _mediaSourceRepository.GetAllJellyfin()
            .Map(list => list.HeadOrNone());

        Option<EmbyMediaSource> maybeEmby = await _mediaSourceRepository.GetAllEmby()
            .Map(list => list.HeadOrNone());

        return await _televisionRepository.GetSeason(request.SeasonId)
            .MapT(s => ProjectToViewModel(s, maybeJellyfin, maybeEmby));
    }
}