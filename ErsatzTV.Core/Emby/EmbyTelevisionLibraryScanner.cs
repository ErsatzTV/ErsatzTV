using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Metadata;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Emby;

public class EmbyTelevisionLibraryScanner : IEmbyTelevisionLibraryScanner
{
    private readonly IEmbyApiClient _embyApiClient;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILocalStatisticsProvider _localStatisticsProvider;
    private readonly ILocalSubtitlesProvider _localSubtitlesProvider;
    private readonly ILogger<EmbyTelevisionLibraryScanner> _logger;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IMediator _mediator;
    private readonly IEmbyPathReplacementService _pathReplacementService;
    private readonly ISearchIndex _searchIndex;
    private readonly ISearchRepository _searchRepository;
    private readonly IEmbyTelevisionRepository _televisionRepository;

    public EmbyTelevisionLibraryScanner(
        IEmbyApiClient embyApiClient,
        IMediaSourceRepository mediaSourceRepository,
        IEmbyTelevisionRepository televisionRepository,
        ISearchIndex searchIndex,
        ISearchRepository searchRepository,
        IEmbyPathReplacementService pathReplacementService,
        ILocalFileSystem localFileSystem,
        ILocalStatisticsProvider localStatisticsProvider,
        ILocalSubtitlesProvider localSubtitlesProvider,
        IMediator mediator,
        ILogger<EmbyTelevisionLibraryScanner> logger)
    {
        _embyApiClient = embyApiClient;
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
        EmbyLibrary library,
        string ffmpegPath,
        string ffprobePath,
        CancellationToken cancellationToken)
    {
        try
        {
            List<EmbyItemEtag> existingShows = await _televisionRepository.GetExistingShows(library);

            // TODO: maybe get quick list of item ids and etags from api to compare first
            // TODO: paging?

            List<EmbyPathReplacement> pathReplacements = await _mediaSourceRepository
                .GetEmbyPathReplacements(library.MediaSourceId);

            Either<BaseError, List<EmbyShow>> maybeShows = await _embyApiClient.GetShowLibraryItems(
                address,
                apiKey,
                library.ItemId);

            foreach (BaseError error in maybeShows.LeftToSeq())
            {
                _logger.LogWarning(
                    "Error synchronizing emby library {Path}: {Error}",
                    library.Name,
                    error.Value);
            }

            foreach (List<EmbyShow> shows in maybeShows.RightToSeq())
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
        EmbyLibrary library,
        string ffmpegPath,
        string ffprobePath,
        List<EmbyPathReplacement> pathReplacements,
        List<EmbyItemEtag> existingShows,
        List<EmbyShow> shows,
        CancellationToken cancellationToken)
    {
        var sortedShows = shows.OrderBy(s => s.ShowMetadata.Head().Title).ToList();
        foreach (EmbyShow incoming in sortedShows)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new ScanCanceled();
            }

            decimal percentCompletion = (decimal)sortedShows.IndexOf(incoming) / shows.Count;
            await _mediator.Publish(new LibraryScanProgress(library.Id, percentCompletion), cancellationToken);

            Option<EmbyItemEtag> maybeExisting = existingShows.Find(ie => ie.ItemId == incoming.ItemId);
            if (maybeExisting.IsNone)
            {
                incoming.LibraryPathId = library.Paths.Head().Id;

                // _logger.LogDebug("INSERT: Item id is new for show {Show}", incoming.ShowMetadata.Head().Title);

                if (await _televisionRepository.AddShow(incoming))
                {
                    await _searchIndex.AddItems(_searchRepository, new List<MediaItem> { incoming });
                }
            }

            foreach (EmbyItemEtag existing in maybeExisting)
            {
                if (existing.Etag != incoming.Etag)
                {
                    _logger.LogDebug("UPDATE: Etag has changed for show {Show}", incoming.ShowMetadata.Head().Title);

                    incoming.LibraryPathId = library.Paths.Head().Id;

                    Option<EmbyShow> maybeUpdated = await _televisionRepository.Update(incoming);
                    foreach (EmbyShow updated in maybeUpdated)
                    {
                        await _searchIndex.UpdateItems(_searchRepository, new List<MediaItem> { updated });
                    }
                }
            }

            List<EmbyItemEtag> existingSeasons =
                await _televisionRepository.GetExistingSeasons(library, incoming.ItemId);

            Either<BaseError, List<EmbySeason>> maybeSeasons =
                await _embyApiClient.GetSeasonLibraryItems(address, apiKey, incoming.ItemId);

            foreach (BaseError error in maybeSeasons.LeftToSeq())
            {
                _logger.LogWarning(
                    "Error synchronizing emby library {Path}: {Error}",
                    library.Name,
                    error.Value);
            }

            foreach (List<EmbySeason> seasons in maybeSeasons.RightToSeq())
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
        EmbyLibrary library,
        string ffmpegPath,
        string ffprobePath,
        List<EmbyPathReplacement> pathReplacements,
        EmbyShow show,
        List<EmbyItemEtag> existingSeasons,
        List<EmbySeason> seasons,
        CancellationToken cancellationToken)
    {
        foreach (EmbySeason incoming in seasons)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new ScanCanceled();
            }

            Option<EmbyItemEtag> maybeExisting = existingSeasons.Find(ie => ie.ItemId == incoming.ItemId);
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

            foreach (EmbyItemEtag existing in maybeExisting)
            {
                if (existing.Etag != incoming.Etag)
                {
                    _logger.LogDebug(
                        "UPDATE: Etag has changed for show {Show} season {Season}",
                        show.ShowMetadata.Head().Title,
                        incoming.SeasonMetadata.Head().Title);

                    incoming.ShowId = show.Id;
                    incoming.LibraryPathId = library.Paths.Head().Id;

                    foreach (EmbySeason updated in await _televisionRepository.Update(incoming))
                    {
                        incoming.Show = show;

                        foreach (MediaItem toIndex in await _searchRepository.GetItemToIndex(updated.Id))
                        {
                            await _searchIndex.UpdateItems(_searchRepository, new List<MediaItem> { toIndex });
                        }
                    }
                }
            }

            List<EmbyItemEtag> existingEpisodes =
                await _televisionRepository.GetExistingEpisodes(library, incoming.ItemId);

            Either<BaseError, List<EmbyEpisode>> maybeEpisodes =
                await _embyApiClient.GetEpisodeLibraryItems(address, apiKey, incoming.ItemId);

            foreach (BaseError error in maybeEpisodes.LeftToSeq())
            {
                _logger.LogWarning(
                    "Error synchronizing emby library {Path}: {Error}",
                    library.Name,
                    error.Value);
            }

            foreach (List<EmbyEpisode> episodes in maybeEpisodes.RightToSeq())
            {
                var validEpisodes = new List<EmbyEpisode>();
                foreach (EmbyEpisode episode in episodes)
                {
                    string localPath = _pathReplacementService.GetReplacementEmbyPath(
                        pathReplacements,
                        episode.MediaVersions.Head().MediaFiles.Head().Path,
                        false);

                    if (!_localFileSystem.FileExists(localPath))
                    {
                        _logger.LogWarning(
                            "Skipping emby episode that does not exist at {Path}",
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
        EmbyLibrary library,
        string ffmpegPath,
        string ffprobePath,
        List<EmbyPathReplacement> pathReplacements,
        EmbySeason season,
        List<EmbyItemEtag> existingEpisodes,
        List<EmbyEpisode> episodes,
        CancellationToken cancellationToken)
    {
        foreach (EmbyEpisode incoming in episodes)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new ScanCanceled();
            }

            EmbyEpisode incomingEpisode = incoming;
            var updateStatistics = false;

            Option<EmbyItemEtag> maybeExisting = existingEpisodes.Find(ie => ie.ItemId == incoming.ItemId);
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

            foreach (EmbyItemEtag existing in maybeExisting)
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

                        Option<EmbyEpisode> maybeUpdated = await _televisionRepository.Update(incoming);
                        foreach (EmbyEpisode updated in maybeUpdated)
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
                string localPath = _pathReplacementService.GetReplacementEmbyPath(
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

    private async Task<Either<BaseError, bool>> UpdateSubtitles(EmbyEpisode episode, string localPath)
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
