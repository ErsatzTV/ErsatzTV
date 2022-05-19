using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using static ErsatzTV.Application.MediaCards.Mapper;

namespace ErsatzTV.Application.MediaCards;

public class
    GetTelevisionSeasonCardsHandler : IRequestHandler<GetTelevisionSeasonCards,
        TelevisionSeasonCardResultsViewModel>
{
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly ITelevisionRepository _televisionRepository;

    public GetTelevisionSeasonCardsHandler(
        ITelevisionRepository televisionRepository,
        IMediaSourceRepository mediaSourceRepository)
    {
        _televisionRepository = televisionRepository;
        _mediaSourceRepository = mediaSourceRepository;
    }

    public async Task<TelevisionSeasonCardResultsViewModel> Handle(
        GetTelevisionSeasonCards request,
        CancellationToken cancellationToken)
    {
        int count = await _televisionRepository.GetSeasonCount(request.TelevisionShowId);

        Option<JellyfinMediaSource> maybeJellyfin = await _mediaSourceRepository.GetAllJellyfin()
            .Map(list => list.HeadOrNone());

        Option<EmbyMediaSource> maybeEmby = await _mediaSourceRepository.GetAllEmby()
            .Map(list => list.HeadOrNone());

        List<TelevisionSeasonCardViewModel> results = await _televisionRepository
            .GetPagedSeasons(request.TelevisionShowId, request.PageNumber, request.PageSize)
            .Map(list => list.Map(s => ProjectToViewModel(s, maybeJellyfin, maybeEmby)).ToList());

        return new TelevisionSeasonCardResultsViewModel(count, results, None);
    }
}
