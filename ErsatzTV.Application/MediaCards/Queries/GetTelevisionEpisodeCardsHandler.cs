using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Search;
using static ErsatzTV.Application.MediaCards.Mapper;

namespace ErsatzTV.Application.MediaCards;

public class
    GetTelevisionEpisodeCardsHandler : IRequestHandler<GetTelevisionEpisodeCards, TelevisionEpisodeCardResultsViewModel>
{
    private readonly IEmbyPathReplacementService _embyPathReplacementService;
    private readonly IJellyfinPathReplacementService _jellyfinPathReplacementService;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IPlexPathReplacementService _plexPathReplacementService;
    private readonly ITelevisionRepository _televisionRepository;

    public GetTelevisionEpisodeCardsHandler(
        ITelevisionRepository televisionRepository,
        IMediaSourceRepository mediaSourceRepository,
        IPlexPathReplacementService plexPathReplacementService,
        IJellyfinPathReplacementService jellyfinPathReplacementService,
        IEmbyPathReplacementService embyPathReplacementService)
    {
        _televisionRepository = televisionRepository;
        _mediaSourceRepository = mediaSourceRepository;
        _plexPathReplacementService = plexPathReplacementService;
        _jellyfinPathReplacementService = jellyfinPathReplacementService;
        _embyPathReplacementService = embyPathReplacementService;
    }

    public async Task<TelevisionEpisodeCardResultsViewModel> Handle(
        GetTelevisionEpisodeCards request,
        CancellationToken cancellationToken)
    {
        int count = await _televisionRepository.GetEpisodeCount(request.TelevisionSeasonId);

        Option<JellyfinMediaSource> maybeJellyfin = await _mediaSourceRepository.GetAllJellyfin()
            .Map(list => list.HeadOrNone());

        Option<EmbyMediaSource> maybeEmby = await _mediaSourceRepository.GetAllEmby()
            .Map(list => list.HeadOrNone());

        List<EpisodeMetadata> episodes = await _televisionRepository
            .GetPagedEpisodes(request.TelevisionSeasonId, request.PageNumber, request.PageSize);

        var results = new List<TelevisionEpisodeCardViewModel>();
        foreach (EpisodeMetadata episodeMetadata in episodes)
        {
            string localPath = await episodeMetadata.Episode.GetLocalPath(
                _plexPathReplacementService,
                _jellyfinPathReplacementService,
                _embyPathReplacementService);

            results.Add(ProjectToViewModel(episodeMetadata, maybeJellyfin, maybeEmby, false, localPath));
        }

        return new TelevisionEpisodeCardResultsViewModel(count, results, Option<SearchPageMap>.None);
    }
}
