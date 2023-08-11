using Bugsnag;
using ErsatzTV.Application.MediaCards;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Search;
using static ErsatzTV.Application.MediaCards.Mapper;

namespace ErsatzTV.Application.Search;

public class
    QuerySearchIndexMusicVideosHandler : IRequestHandler<QuerySearchIndexMusicVideos, MusicVideoCardResultsViewModel>
{
    private readonly IClient _client;
    private readonly IEmbyPathReplacementService _embyPathReplacementService;
    private readonly IJellyfinPathReplacementService _jellyfinPathReplacementService;
    private readonly IMusicVideoRepository _musicVideoRepository;
    private readonly IPlexPathReplacementService _plexPathReplacementService;
    private readonly ISearchIndex _searchIndex;

    public QuerySearchIndexMusicVideosHandler(
        IClient client,
        ISearchIndex searchIndex,
        IMusicVideoRepository musicVideoRepository,
        IPlexPathReplacementService plexPathReplacementService,
        IJellyfinPathReplacementService jellyfinPathReplacementService,
        IEmbyPathReplacementService embyPathReplacementService)
    {
        _client = client;
        _searchIndex = searchIndex;
        _musicVideoRepository = musicVideoRepository;
        _plexPathReplacementService = plexPathReplacementService;
        _jellyfinPathReplacementService = jellyfinPathReplacementService;
        _embyPathReplacementService = embyPathReplacementService;
    }

    public async Task<MusicVideoCardResultsViewModel> Handle(
        QuerySearchIndexMusicVideos request,
        CancellationToken cancellationToken)
    {
        SearchResult searchResult = await _searchIndex.Search(
            _client,
            request.Query,
            (request.PageNumber - 1) * request.PageSize,
            request.PageSize);

        List<MusicVideoMetadata> musicVideos = await _musicVideoRepository
            .GetMusicVideosForCards(searchResult.Items.Map(i => i.Id).ToList());

        var items = new List<MusicVideoCardViewModel>();

        foreach (MusicVideoMetadata musicVideoMetadata in musicVideos)
        {
            string localPath = await musicVideoMetadata.MusicVideo.GetLocalPath(
                _plexPathReplacementService,
                _jellyfinPathReplacementService,
                _embyPathReplacementService,
                false);

            items.Add(ProjectToViewModel(musicVideoMetadata, localPath));
        }

        return new MusicVideoCardResultsViewModel(searchResult.TotalCount, items, searchResult.PageMap);
    }
}
