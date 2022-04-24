using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using static ErsatzTV.Application.MediaCards.Mapper;

namespace ErsatzTV.Application.MediaCards;

public class GetMusicVideoCardsHandler : IRequestHandler<GetMusicVideoCards, MusicVideoCardResultsViewModel>
{
    private readonly IEmbyPathReplacementService _embyPathReplacementService;
    private readonly IJellyfinPathReplacementService _jellyfinPathReplacementService;
    private readonly IMusicVideoRepository _musicVideoRepository;
    private readonly IPlexPathReplacementService _plexPathReplacementService;

    public GetMusicVideoCardsHandler(
        IMusicVideoRepository musicVideoRepository,
        IPlexPathReplacementService plexPathReplacementService,
        IJellyfinPathReplacementService jellyfinPathReplacementService,
        IEmbyPathReplacementService embyPathReplacementService)
    {
        _musicVideoRepository = musicVideoRepository;
        _plexPathReplacementService = plexPathReplacementService;
        _jellyfinPathReplacementService = jellyfinPathReplacementService;
        _embyPathReplacementService = embyPathReplacementService;
    }

    public async Task<MusicVideoCardResultsViewModel> Handle(
        GetMusicVideoCards request,
        CancellationToken cancellationToken)
    {
        int count = await _musicVideoRepository.GetMusicVideoCount(request.ArtistId);

        List<MusicVideoMetadata> musicVideos = await _musicVideoRepository
            .GetPagedMusicVideos(request.ArtistId, request.PageNumber, request.PageSize);

        var results = new List<MusicVideoCardViewModel>();

        foreach (MusicVideoMetadata musicVideoMetadata in musicVideos)
        {
            string localPath = await musicVideoMetadata.MusicVideo.GetLocalPath(
                _plexPathReplacementService,
                _jellyfinPathReplacementService,
                _embyPathReplacementService,
                false);

            results.Add(ProjectToViewModel(musicVideoMetadata, localPath));
        }

        return new MusicVideoCardResultsViewModel(count, results, None);
    }
}
