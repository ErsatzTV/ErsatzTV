using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Metadata;
using LanguageExt.UnsafeValueAccess;
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
        string ffprobePath)
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
            await ProcessShows(
                address,
                apiKey,
                library,
                ffmpegPath,
                ffprobePath,
                pathReplacements,
                existingShows,
                shows);

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

            await _mediator.Publish(new LibraryScanProgress(library.Id, 0));
            _searchIndex.Commit();
        }

        return Unit.Default;
    }

    private async Task ProcessShows(
        string address,
        string apiKey,
        EmbyLibrary library,
        string ffmpegPath,
        string ffprobePath,
        List<EmbyPathReplacement> pathReplacements,
        List<EmbyItemEtag> existingShows,
        List<EmbyShow> shows)
    {
        var sortedShows = shows.OrderBy(s => s.ShowMetadata.Head().Title).ToList();
        foreach (EmbyShow incoming in sortedShows)
        {
            decimal percentCompletion = (decimal)sortedShows.IndexOf(incoming) / shows.Count;
            await _mediator.Publish(new LibraryScanProgress(library.Id, percentCompletion));

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
                if (existing.Etag == incoming.Etag)
                {
                    return;
                }

                _logger.LogDebug("UPDATE: Etag has changed for show {Show}", incoming.ShowMetadata.Head().Title);

                incoming.LibraryPathId = library.Paths.Head().Id;

                Option<EmbyShow> updated = await _televisionRepository.Update(incoming);
                if (updated.IsSome)
                {
                    await _searchIndex.UpdateItems(_searchRepository, new List<MediaItem> { updated.ValueUnsafe() });
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
                await ProcessSeasons(
                    address,
                    apiKey,
                    library,
                    ffmpegPath,
                    ffprobePath,
                    pathReplacements,
                    incoming,
                    existingSeasons,
                    seasons);

                var incomingSeasonIds = seasons.Map(s => s.ItemId).ToList();
                var seasonIds = existingSeasons
                    .Filter(i => !incomingSeasonIds.Contains(i.ItemId))
                    .Map(m => m.ItemId)
                    .ToList();
                await _televisionRepository.RemoveMissingSeasons(library, seasonIds);
            }
        }
    }

    private async Task ProcessSeasons(
        string address,
        string apiKey,
        EmbyLibrary library,
        string ffmpegPath,
        string ffprobePath,
        List<EmbyPathReplacement> pathReplacements,
        EmbyShow show,
        List<EmbyItemEtag> existingSeasons,
        List<EmbySeason> seasons)
    {
        foreach (EmbySeason incoming in seasons)
        {
            Option<EmbyItemEtag> maybeExisting = existingSeasons.Find(ie => ie.ItemId == incoming.ItemId);
            await maybeExisting.Match(
                async existing =>
                {
                    if (existing.Etag == incoming.Etag)
                    {
                        return;
                    }

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
                },
                async () =>
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
                });

            List<EmbyItemEtag> existingEpisodes =
                await _televisionRepository.GetExistingEpisodes(library, incoming.ItemId);

            Either<BaseError, List<EmbyEpisode>> maybeEpisodes =
                await _embyApiClient.GetEpisodeLibraryItems(address, apiKey, incoming.ItemId);

            await maybeEpisodes.Match(
                async episodes =>
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

                    await ProcessEpisodes(
                        show.ShowMetadata.Head().Title,
                        incoming.SeasonMetadata.Head().Title,
                        library,
                        ffmpegPath,
                        ffprobePath,
                        pathReplacements,
                        incoming,
                        existingEpisodes,
                        validEpisodes);

                    var incomingEpisodeIds = episodes.Map(s => s.ItemId).ToList();
                    var episodeIds = existingEpisodes
                        .Filter(i => !incomingEpisodeIds.Contains(i.ItemId))
                        .Map(m => m.ItemId)
                        .ToList();
                    List<int> missingEpisodeIds =
                        await _televisionRepository.RemoveMissingEpisodes(library, episodeIds);
                    await _searchIndex.RemoveItems(missingEpisodeIds);
                    _searchIndex.Commit();
                },
                error =>
                {
                    _logger.LogWarning(
                        "Error synchronizing emby library {Path}: {Error}",
                        library.Name,
                        error.Value);

                    return Task.CompletedTask;
                });
        }
    }

    private async Task ProcessEpisodes(
        string showName,
        string seasonName,
        EmbyLibrary library,
        string ffmpegPath,
        string ffprobePath,
        List<EmbyPathReplacement> pathReplacements,
        EmbySeason season,
        List<EmbyItemEtag> existingEpisodes,
        List<EmbyEpisode> episodes)
    {
        foreach (EmbyEpisode incoming in episodes)
        {
            EmbyEpisode incomingEpisode = incoming;
            var updateStatistics = false;

            Option<EmbyItemEtag> maybeExisting = existingEpisodes.Find(ie => ie.ItemId == incoming.ItemId);
            await maybeExisting.Match(
                async existing =>
                {
                    try
                    {
                        if (existing.Etag == incoming.Etag)
                        {
                            return;
                        }

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
                    catch (Exception ex)
                    {
                        updateStatistics = false;
                        _logger.LogError(
                            ex,
                            "Error updating episode {Path}",
                            incoming.MediaVersions.Head().MediaFiles.Head().Path);
                    }
                },
                async () =>
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
                });

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
