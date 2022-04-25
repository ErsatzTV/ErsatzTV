using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Metadata;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Jellyfin;

public class JellyfinTelevisionLibraryScanner : IJellyfinTelevisionLibraryScanner
{
    private readonly IJellyfinApiClient _jellyfinApiClient;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILocalStatisticsProvider _localStatisticsProvider;
    private readonly ILocalSubtitlesProvider _localSubtitlesProvider;
    private readonly ILogger<JellyfinTelevisionLibraryScanner> _logger;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IMediator _mediator;
    private readonly IJellyfinPathReplacementService _pathReplacementService;
    private readonly ISearchIndex _searchIndex;
    private readonly ISearchRepository _searchRepository;
    private readonly IJellyfinTelevisionRepository _televisionRepository;

    public JellyfinTelevisionLibraryScanner(
        IJellyfinApiClient jellyfinApiClient,
        IMediaSourceRepository mediaSourceRepository,
        IJellyfinTelevisionRepository televisionRepository,
        ISearchIndex searchIndex,
        ISearchRepository searchRepository,
        IJellyfinPathReplacementService pathReplacementService,
        ILocalFileSystem localFileSystem,
        ILocalStatisticsProvider localStatisticsProvider,
        ILocalSubtitlesProvider localSubtitlesProvider,
        IMediator mediator,
        ILogger<JellyfinTelevisionLibraryScanner> logger)
    {
        _jellyfinApiClient = jellyfinApiClient;
        _mediaSourceRepository = mediaSourceRepository;
        _televisionRepository = televisionRepository;
        _searchIndex = searchIndex;
        _searchRepository = searchRepository;
        _pathReplacementService = pathReplacementService;
        _localFileSystem = localFileSystem;
        _localStatisticsProvider = localStatisticsProvider;
        _localSubtitlesProvider = localSubtitlesProvider;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<Either<BaseError, Unit>> ScanLibrary(
        string address,
        string apiKey,
        JellyfinLibrary library,
        string ffmpegPath,
        string ffprobePath,
        CancellationToken cancellationToken)
    {
        try
        {
            List<JellyfinItemEtag> existingShows = await _televisionRepository.GetExistingShows(library);

            // TODO: maybe get quick list of item ids and etags from api to compare first
            // TODO: paging?

            List<JellyfinPathReplacement> pathReplacements = await _mediaSourceRepository
                .GetJellyfinPathReplacements(library.MediaSourceId);

            Either<BaseError, List<JellyfinShow>> maybeShows = await _jellyfinApiClient.GetShowLibraryItems(
                address,
                apiKey,
                library.MediaSourceId,
                library.ItemId);

            foreach (BaseError error in maybeShows.LeftToSeq())
            {
                _logger.LogWarning(
                    "Error synchronizing jellyfin library {Path}: {Error}",
                    library.Name,
                    error.Value);
            }

            foreach (List<JellyfinShow> shows in maybeShows.RightToSeq())
            {
                Either<BaseError, Unit> scanResult = await ProcessShows(
                    address,
                    apiKey,
                    library,
                    ffmpegPath,
                    ffprobePath,
                    pathReplacements,
                    existingShows,
                    shows,
                    cancellationToken);

                foreach (ScanCanceled error in scanResult.LeftToSeq().OfType<ScanCanceled>())
                {
                    return error;
                }

                foreach (Unit _ in scanResult.RightToSeq())
                {
                    var incomingShowIds = shows.Map(s => s.ItemId).ToList();
                    var showIds = existingShows
                        .Filter(i => !incomingShowIds.Contains(i.ItemId))
                        .Map(m => m.ItemId)
                        .ToList();
                    List<int> missingShowIds = await _televisionRepository.RemoveMissingShows(library, showIds);
                    await _searchIndex.RemoveItems(missingShowIds);

                    await _televisionRepository.DeleteEmptySeasons(library);
                    List<int> emptyShowIds = await _televisionRepository.DeleteEmptyShows(library);
                    await _searchIndex.RemoveItems(emptyShowIds);

                    await _mediator.Publish(new LibraryScanProgress(library.Id, 0), cancellationToken);
                }
            }

            return Unit.Default;
        }
        catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
        {
            return new ScanCanceled();
        }
        finally
        {
            _searchIndex.Commit();
        }
    }

    private async Task<Either<BaseError, Unit>> ProcessShows(
        string address,
        string apiKey,
        JellyfinLibrary library,
        string ffmpegPath,
        string ffprobePath,
        List<JellyfinPathReplacement> pathReplacements,
        List<JellyfinItemEtag> existingShows,
        List<JellyfinShow> shows,
        CancellationToken cancellationToken)
    {
        var sortedShows = shows.OrderBy(s => s.ShowMetadata.Head().Title).ToList();
        foreach (JellyfinShow incoming in sortedShows)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new ScanCanceled();
            }

            decimal percentCompletion = (decimal)sortedShows.IndexOf(incoming) / shows.Count;
            await _mediator.Publish(new LibraryScanProgress(library.Id, percentCompletion), cancellationToken);

            Option<JellyfinItemEtag> maybeExisting = existingShows.Find(ie => ie.ItemId == incoming.ItemId);
            if (maybeExisting.IsNone)
            {
                incoming.LibraryPathId = library.Paths.Head().Id;

                // _logger.LogDebug("INSERT: Item id is new for show {Show}", incoming.ShowMetadata.Head().Title);

                if (await _televisionRepository.AddShow(incoming))
                {
                    await _searchIndex.AddItems(_searchRepository, new List<MediaItem> { incoming });
                }
            }

            foreach (JellyfinItemEtag existing in maybeExisting)
            {
                if (existing.Etag != incoming.Etag)
                {
                    _logger.LogDebug("UPDATE: Etag has changed for show {Show}", incoming.ShowMetadata.Head().Title);

                    incoming.LibraryPathId = library.Paths.Head().Id;

                    Option<JellyfinShow> maybeUpdated = await _televisionRepository.Update(incoming);
                    foreach (JellyfinShow updated in maybeUpdated)
                    {
                        await _searchIndex.UpdateItems(_searchRepository, new List<MediaItem> { updated });
                    }
                }
            }

            List<JellyfinItemEtag> existingSeasons =
                await _televisionRepository.GetExistingSeasons(library, incoming.ItemId);

            Either<BaseError, List<JellyfinSeason>> maybeSeasons =
                await _jellyfinApiClient.GetSeasonLibraryItems(address, apiKey, library.MediaSourceId, incoming.ItemId);

            foreach (BaseError error in maybeSeasons.LeftToSeq())
            {
                _logger.LogWarning(
                    "Error synchronizing jellyfin library {Path}: {Error}",
                    library.Name,
                    error.Value);
            }

            foreach (List<JellyfinSeason> seasons in maybeSeasons.RightToSeq())
            {
                Either<BaseError, Unit> scanResult = await ProcessSeasons(
                    address,
                    apiKey,
                    library,
                    ffmpegPath,
                    ffprobePath,
                    pathReplacements,
                    incoming,
                    existingSeasons,
                    seasons,
                    cancellationToken);

                foreach (ScanCanceled error in scanResult.LeftToSeq().OfType<ScanCanceled>())
                {
                    return error;
                }

                foreach (Unit _ in scanResult.RightToSeq())
                {
                    var incomingSeasonIds = seasons.Map(s => s.ItemId).ToList();
                    var seasonIds = existingSeasons
                        .Filter(i => !incomingSeasonIds.Contains(i.ItemId))
                        .Map(m => m.ItemId)
                        .ToList();
                    await _televisionRepository.RemoveMissingSeasons(library, seasonIds);
                }
            }
        }

        return Unit.Default;
    }

    private async Task<Either<BaseError, Unit>> ProcessSeasons(
        string address,
        string apiKey,
        JellyfinLibrary library,
        string ffmpegPath,
        string ffprobePath,
        List<JellyfinPathReplacement> pathReplacements,
        JellyfinShow show,
        List<JellyfinItemEtag> existingSeasons,
        List<JellyfinSeason> seasons,
        CancellationToken cancellationToken)
    {
        foreach (JellyfinSeason incoming in seasons)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new ScanCanceled();
            }

            Option<JellyfinItemEtag> maybeExisting = existingSeasons.Find(ie => ie.ItemId == incoming.ItemId);
            if (maybeExisting.IsNone)
            {
                incoming.LibraryPathId = library.Paths.Head().Id;

                _logger.LogDebug(
                    "INSERT: Item id is new for show {Show} season {Season}",
                    show.ShowMetadata.Head().Title,
                    incoming.SeasonMetadata.Head().Title);

                if (await _televisionRepository.AddSeason(show, incoming))
                {
                    incoming.Show = show;
                    await _searchIndex.AddItems(_searchRepository, new List<MediaItem> { incoming });
                }
            }

            foreach (JellyfinItemEtag existing in maybeExisting)
            {
                if (existing.Etag != incoming.Etag)
                {
                    _logger.LogDebug(
                        "UPDATE: Etag has changed for show {Show} season {Season}",
                        show.ShowMetadata.Head().Title,
                        incoming.SeasonMetadata.Head().Title);

                    incoming.ShowId = show.Id;
                    incoming.LibraryPathId = library.Paths.Head().Id;

                    foreach (JellyfinSeason updated in await _televisionRepository.Update(incoming))
                    {
                        incoming.Show = show;

                        foreach (MediaItem toIndex in await _searchRepository.GetItemToIndex(updated.Id))
                        {
                            await _searchIndex.UpdateItems(_searchRepository, new List<MediaItem> { toIndex });
                        }
                    }
                }
            }

            List<JellyfinItemEtag> existingEpisodes =
                await _televisionRepository.GetExistingEpisodes(library, incoming.ItemId);

            Either<BaseError, List<JellyfinEpisode>> maybeEpisodes =
                await _jellyfinApiClient.GetEpisodeLibraryItems(
                    address,
                    apiKey,
                    library.MediaSourceId,
                    incoming.ItemId);

            foreach (BaseError error in maybeEpisodes.LeftToSeq())
            {
                _logger.LogWarning(
                    "Error synchronizing jellyfin library {Path}: {Error}",
                    library.Name,
                    error.Value);
            }

            foreach (List<JellyfinEpisode> episodes in maybeEpisodes.RightToSeq())
            {
                var validEpisodes = new List<JellyfinEpisode>();
                foreach (JellyfinEpisode episode in episodes)
                {
                    string localPath = _pathReplacementService.GetReplacementJellyfinPath(
                        pathReplacements,
                        episode.MediaVersions.Head().MediaFiles.Head().Path,
                        false);

                    if (!_localFileSystem.FileExists(localPath))
                    {
                        _logger.LogWarning(
                            "Skipping jellyfin episode that does not exist at {Path}",
                            localPath);
                    }
                    else
                    {
                        validEpisodes.Add(episode);
                    }
                }

                Either<BaseError, Unit> scanResult = await ProcessEpisodes(
                    show.ShowMetadata.Head().Title,
                    incoming.SeasonMetadata.Head().Title,
                    library,
                    ffmpegPath,
                    ffprobePath,
                    pathReplacements,
                    incoming,
                    existingEpisodes,
                    validEpisodes,
                    cancellationToken);

                foreach (ScanCanceled error in scanResult.LeftToSeq().OfType<ScanCanceled>())
                {
                    return error;
                }

                foreach (Unit _ in scanResult.RightToSeq())
                {
                    var incomingEpisodeIds = episodes.Map(s => s.ItemId).ToList();
                    var episodeIds = existingEpisodes
                        .Filter(i => !incomingEpisodeIds.Contains(i.ItemId))
                        .Map(m => m.ItemId)
                        .ToList();
                    List<int> missingEpisodeIds =
                        await _televisionRepository.RemoveMissingEpisodes(library, episodeIds);
                    await _searchIndex.RemoveItems(missingEpisodeIds);
                    _searchIndex.Commit();
                }
            }
        }

        return Unit.Default;
    }

    private async Task<Either<BaseError, Unit>> ProcessEpisodes(
        string showName,
        string seasonName,
        JellyfinLibrary library,
        string ffmpegPath,
        string ffprobePath,
        List<JellyfinPathReplacement> pathReplacements,
        JellyfinSeason season,
        List<JellyfinItemEtag> existingEpisodes,
        List<JellyfinEpisode> episodes,
        CancellationToken cancellationToken)
    {
        foreach (JellyfinEpisode incoming in episodes)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new ScanCanceled();
            }

            JellyfinEpisode incomingEpisode = incoming;
            var updateStatistics = false;

            Option<JellyfinItemEtag> maybeExisting = existingEpisodes.Find(ie => ie.ItemId == incoming.ItemId);
            if (maybeExisting.IsNone)
            {
                try
                {
                    updateStatistics = true;
                    incoming.LibraryPathId = library.Paths.Head().Id;

                    _logger.LogDebug(
                        "INSERT: Item id is new for show {Show} season {Season} episode {Episode}",
                        showName,
                        seasonName,
                        incoming.EpisodeMetadata.HeadOrNone().Map(em => em.EpisodeNumber));

                    if (await _televisionRepository.AddEpisode(season, incoming))
                    {
                        await _searchIndex.AddItems(_searchRepository, new List<MediaItem> { incoming });
                    }
                }
                catch (Exception ex)
                {
                    updateStatistics = false;
                    _logger.LogError(
                        ex,
                        "Error adding episode {Path}",
                        incoming.MediaVersions.Head().MediaFiles.Head().Path);
                }
            }

            foreach (JellyfinItemEtag existing in maybeExisting)
            {
                try
                {
                    if (existing.Etag != incoming.Etag)
                    {
                        _logger.LogDebug(
                            "UPDATE: Etag has changed for show {Show} season {Season} episode {Episode}",
                            showName,
                            seasonName,
                            incoming.EpisodeMetadata.HeadOrNone().Map(em => em.EpisodeNumber));

                        updateStatistics = true;
                        incoming.SeasonId = season.Id;
                        incoming.LibraryPathId = library.Paths.Head().Id;

                        Option<JellyfinEpisode> maybeUpdated = await _televisionRepository.Update(incoming);
                        foreach (JellyfinEpisode updated in maybeUpdated)
                        {
                            await _searchIndex.UpdateItems(_searchRepository, new List<MediaItem> { updated });
                            incomingEpisode = updated;
                        }
                    }
                }
                catch (Exception ex)
                {
                    updateStatistics = false;
                    _logger.LogError(
                        ex,
                        "Error updating episode {Path}",
                        incoming.MediaVersions.Head().MediaFiles.Head().Path);
                }
            }

            if (updateStatistics)
            {
                string localPath = _pathReplacementService.GetReplacementJellyfinPath(
                    pathReplacements,
                    incoming.MediaVersions.Head().MediaFiles.Head().Path,
                    false);

                _logger.LogDebug("Refreshing {Attribute} for {Path}", "Statistics", localPath);
                Either<BaseError, bool> refreshResult =
                    await _localStatisticsProvider.RefreshStatistics(
                        ffmpegPath,
                        ffprobePath,
                        incomingEpisode,
                        localPath);

                if (refreshResult.Map(t => t).IfLeft(false))
                {
                    refreshResult = await UpdateSubtitles(incomingEpisode, localPath);
                }

                foreach (BaseError error in refreshResult.LeftToSeq())
                {
                    _logger.LogWarning(
                        "Unable to refresh {Attribute} for media item {Path}. Error: {Error}",
                        "Statistics",
                        localPath,
                        error.Value);
                }
            }
        }

        return Unit.Default;
    }

    private async Task<Either<BaseError, bool>> UpdateSubtitles(JellyfinEpisode episode, string localPath)
    {
        try
        {
            return await _localSubtitlesProvider.UpdateSubtitles(episode, localPath, false);
        }
        catch (Exception ex)
        {
            return BaseError.New(ex.ToString());
        }
    }
}
