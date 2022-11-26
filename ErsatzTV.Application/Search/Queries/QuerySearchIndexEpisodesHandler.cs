using ErsatzTV.Application.MediaCards;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Search;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCards.Mapper;

namespace ErsatzTV.Application.Search;

public class
    QuerySearchIndexEpisodesHandler : IRequestHandler<QuerySearchIndexEpisodes, TelevisionEpisodeCardResultsViewModel>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly IEmbyPathReplacementService _embyPathReplacementService;
    private readonly IFallbackMetadataProvider _fallbackMetadataProvider;
    private readonly IJellyfinPathReplacementService _jellyfinPathReplacementService;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IPlexPathReplacementService _plexPathReplacementService;
    private readonly ISearchIndex _searchIndex;
    private readonly ITelevisionRepository _televisionRepository;

    public QuerySearchIndexEpisodesHandler(
        ISearchIndex searchIndex,
        ITelevisionRepository televisionRepository,
        IMediaSourceRepository mediaSourceRepository,
        IPlexPathReplacementService plexPathReplacementService,
        IJellyfinPathReplacementService jellyfinPathReplacementService,
        IEmbyPathReplacementService embyPathReplacementService,
        IFallbackMetadataProvider fallbackMetadataProvider,
        IDbContextFactory<TvContext> dbContextFactory)
    {
        _searchIndex = searchIndex;
        _televisionRepository = televisionRepository;
        _mediaSourceRepository = mediaSourceRepository;
        _plexPathReplacementService = plexPathReplacementService;
        _jellyfinPathReplacementService = jellyfinPathReplacementService;
        _embyPathReplacementService = embyPathReplacementService;
        _fallbackMetadataProvider = fallbackMetadataProvider;
        _dbContextFactory = dbContextFactory;
    }

    public async Task<TelevisionEpisodeCardResultsViewModel> Handle(
        QuerySearchIndexEpisodes request,
        CancellationToken cancellationToken)
    {
        SearchResult searchResult = _searchIndex.Search(
            request.Query,
            (request.PageNumber - 1) * request.PageSize,
            request.PageSize);

        Option<JellyfinMediaSource> maybeJellyfin = await _mediaSourceRepository.GetAllJellyfin()
            .Map(list => list.HeadOrNone());

        Option<EmbyMediaSource> maybeEmby = await _mediaSourceRepository.GetAllEmby()
            .Map(list => list.HeadOrNone());

        var episodeIds = searchResult.Items.Map(i => i.Id).ToList();

        List<EpisodeMetadata> episodes = await _televisionRepository.GetEpisodesForCards(episodeIds);

        // try to load fallback metadata for episodes that have none
        // this handles an edge case of trashed items with no saved metadata
        var missingEpisodes = episodeIds.Except(episodes.Map(e => e.EpisodeId)).ToList();
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        foreach (int missingEpisodeId in missingEpisodes)
        {
            Option<Episode> maybeEpisode = await dbContext.Episodes
                .AsNoTracking()
                .Include(e => e.MediaVersions)
                .ThenInclude(e => e.MediaFiles)
                .Include(e => e.Season)
                .ThenInclude(s => s.SeasonMetadata)
                .ThenInclude(sm => sm.Artwork)
                .Include(e => e.Season)
                .ThenInclude(s => s.Show)
                .ThenInclude(s => s.ShowMetadata)
                .ThenInclude(sm => sm.Artwork)
                .SelectOneAsync(e => e.Id, e => e.Id == missingEpisodeId);

            foreach (Episode episode in maybeEpisode)
            {
                foreach (EpisodeMetadata headMetadata in _fallbackMetadataProvider.GetFallbackMetadata(episode)
                             .HeadOrNone())
                {
                    headMetadata.Episode = episode;
                    episode.EpisodeMetadata = new List<EpisodeMetadata> { headMetadata };
                    episodes.Add(headMetadata);
                }
            }
        }

        var items = new List<TelevisionEpisodeCardViewModel>();

        foreach (EpisodeMetadata episodeMetadata in episodes)
        {
            string localPath = await episodeMetadata.Episode.GetLocalPath(
                _plexPathReplacementService,
                _jellyfinPathReplacementService,
                _embyPathReplacementService,
                false);

            items.Add(ProjectToViewModel(episodeMetadata, maybeJellyfin, maybeEmby, true, localPath));
        }

        return new TelevisionEpisodeCardResultsViewModel(searchResult.TotalCount, items, searchResult.PageMap);
    }
}
