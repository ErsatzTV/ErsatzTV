using Bugsnag;
using ErsatzTV.Application.MediaCards;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Search;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCards.Mapper;

namespace ErsatzTV.Application.Search;

public class
    QuerySearchIndexEpisodesHandler(
        IClient client,
        ISearchIndex searchIndex,
        IPlexPathReplacementService plexPathReplacementService,
        IJellyfinPathReplacementService jellyfinPathReplacementService,
        IEmbyPathReplacementService embyPathReplacementService,
        IFallbackMetadataProvider fallbackMetadataProvider,
        IDbContextFactory<TvContext> dbContextFactory)
    : QuerySearchIndexHandlerBase, IRequestHandler<QuerySearchIndexEpisodes, TelevisionEpisodeCardResultsViewModel>
{
    public async Task<TelevisionEpisodeCardResultsViewModel> Handle(
        QuerySearchIndexEpisodes request,
        CancellationToken cancellationToken)
    {
        SearchResult searchResult = await searchIndex.Search(
            client,
            request.Query,
            string.Empty,
            (request.PageNumber - 1) * request.PageSize,
            request.PageSize,
            cancellationToken);

        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Option<JellyfinMediaSource> maybeJellyfin = await GetJellyfin(dbContext, cancellationToken);
        Option<EmbyMediaSource> maybeEmby = await GetEmby(dbContext, cancellationToken);

        var episodeIds = searchResult.Items.Map(i => i.Id).ToHashSet();

        List<EpisodeMetadata> episodes = await dbContext.EpisodeMetadata
            .AsNoTracking()
            .Filter(em => episodeIds.Contains(em.EpisodeId))
            .Include(em => em.Artwork)
            .Include(em => em.Directors)
            .Include(em => em.Writers)
            .Include(em => em.Episode)
            .ThenInclude(e => e.Season)
            .ThenInclude(s => s.SeasonMetadata)
            .ThenInclude(sm => sm.Artwork)
            .Include(em => em.Episode)
            .ThenInclude(e => e.Season)
            .ThenInclude(s => s.Show)
            .ThenInclude(s => s.ShowMetadata)
            .ThenInclude(sm => sm.Artwork)
            .Include(em => em.Episode)
            .ThenInclude(e => e.MediaVersions)
            .ThenInclude(mv => mv.MediaFiles)
            .OrderBy(em => em.SortTitle)
            .ToListAsync(cancellationToken);

        // try to load fallback metadata for episodes that have none
        // this handles an edge case of trashed items with no saved metadata
        var missingEpisodes = episodeIds.Except(episodes.Map(e => e.EpisodeId)).ToList();
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
                .SelectOneAsync(e => e.Id, e => e.Id == missingEpisodeId, cancellationToken);

            foreach (Episode episode in maybeEpisode)
            {
                foreach (EpisodeMetadata headMetadata in fallbackMetadataProvider.GetFallbackMetadata(episode)
                             .HeadOrNone())
                {
                    headMetadata.Episode = episode;
                    episode.EpisodeMetadata = [headMetadata];
                    episodes.Add(headMetadata);
                }
            }
        }

        var items = new List<TelevisionEpisodeCardViewModel>();

        foreach (EpisodeMetadata episodeMetadata in episodes)
        {
            string localPath = await episodeMetadata.Episode.GetLocalPath(
                plexPathReplacementService,
                jellyfinPathReplacementService,
                embyPathReplacementService,
                cancellationToken,
                false);

            items.Add(ProjectToViewModel(episodeMetadata, maybeJellyfin, maybeEmby, true, localPath));
        }

        return new TelevisionEpisodeCardResultsViewModel(searchResult.TotalCount, items, searchResult.PageMap);
    }
}
