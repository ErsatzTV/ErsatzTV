using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.MediaServer;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Metadata;

public abstract class MediaServerTelevisionLibraryScanner<TConnectionParameters, TLibrary, TShow, TSeason, TEpisode,
    TEtag>
    where TConnectionParameters : MediaServerConnectionParameters
    where TLibrary : Library
    where TShow : Show
    where TSeason : Season
    where TEpisode : Episode
    where TEtag : MediaServerItemEtag
{
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILocalStatisticsProvider _localStatisticsProvider;
    private readonly ILocalSubtitlesProvider _localSubtitlesProvider;
    private readonly ILogger _logger;
    private readonly IMediator _mediator;
    private readonly ISearchIndex _searchIndex;
    private readonly ISearchRepository _searchRepository;

    protected MediaServerTelevisionLibraryScanner(
        ILocalStatisticsProvider localStatisticsProvider,
        ILocalSubtitlesProvider localSubtitlesProvider,
        ILocalFileSystem localFileSystem,
        ISearchRepository searchRepository,
        ISearchIndex searchIndex,
        IMediator mediator,
        ILogger logger)
    {
        _localStatisticsProvider = localStatisticsProvider;
        _localSubtitlesProvider = localSubtitlesProvider;
        _localFileSystem = localFileSystem;
        _searchRepository = searchRepository;
        _searchIndex = searchIndex;
        _mediator = mediator;
        _logger = logger;
    }

    protected async Task<Either<BaseError, Unit>> ScanLibrary(
        IMediaServerTelevisionRepository<TLibrary, TShow, TSeason, TEpisode, TEtag> televisionRepository,
        TConnectionParameters connectionParameters,
        TLibrary library,
        Func<TEpisode, string> getLocalPath,
        string ffmpegPath,
        string ffprobePath,
        bool deepScan,
        CancellationToken cancellationToken)
    {
        try
        {
            Either<BaseError, List<TShow>> entries = await GetShowLibraryItems(connectionParameters, library);

            foreach (BaseError error in entries.LeftToSeq())
            {
                return error;
            }

            return await ScanLibrary(
                televisionRepository,
                connectionParameters,
                library,
                getLocalPath,
                ffmpegPath,
                ffprobePath,
                entries.RightToSeq().Flatten().ToList(),
                deepScan,
                cancellationToken);
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

    protected abstract Task<Either<BaseError, List<TShow>>> GetShowLibraryItems(
        TConnectionParameters connectionParameters,
        TLibrary library);

    protected abstract string MediaServerItemId(TShow show);
    protected abstract string MediaServerItemId(TSeason season);
    protected abstract string MediaServerItemId(TEpisode episode);
    protected abstract string MediaServerEtag(TShow show);
    protected abstract string MediaServerEtag(TSeason season);
    protected abstract string MediaServerEtag(TEpisode episode);

    private async Task<Either<BaseError, Unit>> ScanLibrary(
        IMediaServerTelevisionRepository<TLibrary, TShow, TSeason, TEpisode, TEtag> televisionRepository,
        TConnectionParameters connectionParameters,
        TLibrary library,
        Func<TEpisode, string> getLocalPath,
        string ffmpegPath,
        string ffprobePath,
        List<TShow> showEntries,
        bool deepScan,
        CancellationToken cancellationToken)
    {
        List<TEtag> existingShows = await televisionRepository.GetExistingShows(library);

        var sortedShows = showEntries.OrderBy(s => s.ShowMetadata.Head().SortTitle).ToList();
        foreach (TShow incoming in showEntries)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new ScanCanceled();
            }

            decimal percentCompletion = (decimal)sortedShows.IndexOf(incoming) / sortedShows.Count;
            await _mediator.Publish(new LibraryScanProgress(library.Id, percentCompletion), cancellationToken);

            Either<BaseError, MediaItemScanResult<TShow>> maybeShow = await televisionRepository
                .GetOrAdd(library, incoming)
                .BindT(existing => UpdateMetadata(connectionParameters, library, existing, incoming, deepScan));

            if (maybeShow.IsLeft)
            {
                foreach (BaseError error in maybeShow.LeftToSeq())
                {
                    _logger.LogWarning(
                        "Error processing show {Title}: {Error}",
                        incoming.ShowMetadata.Head().Title,
                        error.Value);
                }

                continue;
            }

            foreach (MediaItemScanResult<TShow> result in maybeShow.RightToSeq())
            {
                Either<BaseError, List<TSeason>> entries = await GetSeasonLibraryItems(
                    library,
                    connectionParameters,
                    result.Item);

                foreach (BaseError error in entries.LeftToSeq())
                {
                    return error;
                }

                Either<BaseError, Unit> scanResult = await ScanSeasons(
                    televisionRepository,
                    library,
                    getLocalPath,
                    result.Item,
                    connectionParameters,
                    ffmpegPath,
                    ffprobePath,
                    entries.RightToSeq().Flatten().ToList(),
                    deepScan,
                    cancellationToken);

                foreach (ScanCanceled error in scanResult.LeftToSeq().OfType<ScanCanceled>())
                {
                    return error;
                }

                await televisionRepository.SetEtag(result.Item, MediaServerEtag(incoming));

                if (result.IsAdded)
                {
                    await _searchIndex.AddItems(_searchRepository, new List<MediaItem> { result.Item });
                }
                else if (result.IsUpdated)
                {
                    await _searchIndex.UpdateItems(_searchRepository, new List<MediaItem> { result.Item });
                }
            }
        }

        // trash shows that are no longer present on the media server
        var fileNotFoundItemIds = existingShows.Map(s => s.MediaServerItemId)
            .Except(showEntries.Map(MediaServerItemId)).ToList();
        List<int> ids = await televisionRepository.FlagFileNotFoundShows(library, fileNotFoundItemIds);
        await _searchIndex.RebuildItems(_searchRepository, ids);

        await _mediator.Publish(new LibraryScanProgress(library.Id, 0), cancellationToken);

        return Unit.Default;
    }

    protected abstract Task<Either<BaseError, List<TSeason>>> GetSeasonLibraryItems(
        TLibrary library,
        TConnectionParameters connectionParameters,
        TShow show);

    protected abstract Task<Either<BaseError, List<TEpisode>>> GetEpisodeLibraryItems(
        TLibrary library,
        TConnectionParameters connectionParameters,
        TSeason season);

    protected abstract Task<Option<ShowMetadata>> GetFullMetadata(
        TConnectionParameters connectionParameters,
        TLibrary library,
        MediaItemScanResult<TShow> result,
        TShow incoming,
        bool deepScan);

    protected abstract Task<Option<SeasonMetadata>> GetFullMetadata(
        TConnectionParameters connectionParameters,
        TLibrary library,
        MediaItemScanResult<TSeason> result,
        TSeason incoming,
        bool deepScan);

    protected abstract Task<Option<EpisodeMetadata>> GetFullMetadata(
        TConnectionParameters connectionParameters,
        TLibrary library,
        MediaItemScanResult<TEpisode> result,
        TEpisode incoming,
        bool deepScan);

    protected abstract Task<Either<BaseError, MediaItemScanResult<TShow>>> UpdateMetadata(
        MediaItemScanResult<TShow> result,
        ShowMetadata fullMetadata);

    protected abstract Task<Either<BaseError, MediaItemScanResult<TSeason>>> UpdateMetadata(
        MediaItemScanResult<TSeason> result,
        SeasonMetadata fullMetadata);

    protected abstract Task<Either<BaseError, MediaItemScanResult<TEpisode>>> UpdateMetadata(
        MediaItemScanResult<TEpisode> result,
        EpisodeMetadata fullMetadata);

    private async Task<Either<BaseError, Unit>> ScanSeasons(
        IMediaServerTelevisionRepository<TLibrary, TShow, TSeason, TEpisode, TEtag> televisionRepository,
        TLibrary library,
        Func<TEpisode, string> getLocalPath,
        TShow show,
        TConnectionParameters connectionParameters,
        string ffmpegPath,
        string ffprobePath,
        List<TSeason> seasonEntries,
        bool deepScan,
        CancellationToken cancellationToken)
    {
        List<TEtag> existingSeasons = await televisionRepository.GetExistingSeasons(library, show);

        var sortedSeasons = seasonEntries.OrderBy(s => s.SeasonNumber).ToList();
        foreach (TSeason incoming in sortedSeasons)
        {
            incoming.ShowId = show.Id;

            if (cancellationToken.IsCancellationRequested)
            {
                return new ScanCanceled();
            }

            Either<BaseError, MediaItemScanResult<TSeason>> maybeSeason = await televisionRepository
                .GetOrAdd(library, incoming)
                .BindT(existing => UpdateMetadata(connectionParameters, library, existing, incoming, deepScan));

            if (maybeSeason.IsLeft)
            {
                foreach (BaseError error in maybeSeason.LeftToSeq())
                {
                    _logger.LogWarning(
                        "Error processing show {Title} season {SeasonNumber}: {Error}",
                        show.ShowMetadata.Head().Title,
                        incoming.SeasonNumber,
                        error.Value);
                }

                continue;
            }

            foreach (MediaItemScanResult<TSeason> result in maybeSeason.RightToSeq())
            {
                Either<BaseError, List<TEpisode>> entries = await GetEpisodeLibraryItems(
                    library,
                    connectionParameters,
                    result.Item);

                foreach (BaseError error in entries.LeftToSeq())
                {
                    return error;
                }

                Either<BaseError, Unit> scanResult = await ScanEpisodes(
                    televisionRepository,
                    library,
                    getLocalPath,
                    show,
                    result.Item,
                    connectionParameters,
                    ffmpegPath,
                    ffprobePath,
                    entries.RightToSeq().Flatten().ToList(),
                    deepScan,
                    cancellationToken);

                foreach (ScanCanceled error in scanResult.LeftToSeq().OfType<ScanCanceled>())
                {
                    return error;
                }

                await televisionRepository.SetEtag(result.Item, MediaServerEtag(incoming));

                result.Item.Show = show;

                if (result.IsAdded)
                {
                    await _searchIndex.AddItems(_searchRepository, new List<MediaItem> { result.Item });
                }
                else if (result.IsUpdated)
                {
                    await _searchIndex.UpdateItems(_searchRepository, new List<MediaItem> { result.Item });
                }
            }
        }

        // trash seasons that are no longer present on the media server
        var fileNotFoundItemIds = existingSeasons.Map(s => s.MediaServerItemId)
            .Except(seasonEntries.Map(MediaServerItemId)).ToList();
        List<int> ids = await televisionRepository.FlagFileNotFoundSeasons(library, fileNotFoundItemIds);
        await _searchIndex.RebuildItems(_searchRepository, ids);

        return Unit.Default;
    }

    private async Task<Either<BaseError, Unit>> ScanEpisodes(
        IMediaServerTelevisionRepository<TLibrary, TShow, TSeason, TEpisode, TEtag> televisionRepository,
        TLibrary library,
        Func<TEpisode, string> getLocalPath,
        TShow show,
        TSeason season,
        TConnectionParameters connectionParameters,
        string ffmpegPath,
        string ffprobePath,
        List<TEpisode> episodeEntries,
        bool deepScan,
        CancellationToken cancellationToken)
    {
        List<TEtag> existingEpisodes = await televisionRepository.GetExistingEpisodes(library, season);

        var sortedEpisodes = episodeEntries.OrderBy(s => s.EpisodeMetadata.Head().EpisodeNumber).ToList();
        foreach (TEpisode incoming in sortedEpisodes)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new ScanCanceled();
            }

            string localPath = getLocalPath(incoming);
            if (await ShouldScanItem(
                    televisionRepository,
                    library,
                    show,
                    season,
                    existingEpisodes,
                    incoming,
                    localPath,
                    deepScan) == false)
            {
                continue;
            }

            incoming.SeasonId = season.Id;

            Either<BaseError, MediaItemScanResult<TEpisode>> maybeEpisode = await televisionRepository
                .GetOrAdd(library, incoming)
                .MapT(
                    result =>
                    {
                        result.LocalPath = localPath;
                        return result;
                    })
                .BindT(existing => UpdateMetadata(connectionParameters, library, existing, incoming, deepScan))
                .BindT(existing => UpdateStatistics(existing, incoming, ffmpegPath, ffprobePath))
                .BindT(UpdateSubtitles);

            if (maybeEpisode.IsLeft)
            {
                foreach (BaseError error in maybeEpisode.LeftToSeq())
                {
                    _logger.LogWarning(
                        "Error processing episode {Title} s{SeasonNumber:00}e{EpisodeNumber:00}: {Error}",
                        show.ShowMetadata.Head().Title,
                        season.SeasonNumber,
                        incoming.EpisodeMetadata.Head().EpisodeNumber,
                        error.Value);
                }

                continue;
            }

            foreach (MediaItemScanResult<TEpisode> result in maybeEpisode.RightToSeq())
            {
                await televisionRepository.SetEtag(result.Item, MediaServerEtag(incoming));

                if (_localFileSystem.FileExists(result.LocalPath))
                {
                    if (await televisionRepository.FlagNormal(library, result.Item))
                    {
                        result.IsUpdated = true;
                    }
                }
                else
                {
                    Option<int> flagResult = await televisionRepository.FlagUnavailable(library, result.Item);
                    if (flagResult.IsSome)
                    {
                        result.IsUpdated = true;
                    }
                }

                if (result.IsAdded)
                {
                    await _searchIndex.AddItems(_searchRepository, new List<MediaItem> { result.Item });
                }
                else if (result.IsUpdated)
                {
                    await _searchIndex.UpdateItems(_searchRepository, new List<MediaItem> { result.Item });
                }
            }
        }

        // trash episodes that are no longer present on the media server
        var fileNotFoundItemIds = existingEpisodes.Map(m => m.MediaServerItemId)
            .Except(episodeEntries.Map(MediaServerItemId)).ToList();
        List<int> ids = await televisionRepository.FlagFileNotFoundEpisodes(library, fileNotFoundItemIds);
        await _searchIndex.RebuildItems(_searchRepository, ids);

        return Unit.Default;
    }

    private async Task<bool> ShouldScanItem(
        IMediaServerTelevisionRepository<TLibrary, TShow, TSeason, TEpisode, TEtag> televisionRepository,
        TLibrary library,
        Show show,
        Season season,
        List<TEtag> existingEpisodes,
        TEpisode incoming,
        string localPath,
        bool deepScan)
    {
        // deep scan will always pull every episode
        if (deepScan)
        {
            return true;
        }

        Option<TEtag> maybeExisting = existingEpisodes.Find(m => m.MediaServerItemId == MediaServerItemId(incoming));
        string existingItemId = await maybeExisting.Map(e => e.MediaServerItemId).IfNoneAsync(string.Empty);
        MediaItemState existingState = await maybeExisting.Map(e => e.State).IfNoneAsync(MediaItemState.Normal);

        if (existingState == MediaItemState.Unavailable)
        {
            // skip scanning unavailable items that still don't exist locally
            if (!_localFileSystem.FileExists(localPath))
            {
                return false;
            }
        }
        else if (existingItemId == MediaServerItemId(incoming))
        {
            // item is unchanged, but file does not exist
            // don't scan, but mark as unavailable
            if (!_localFileSystem.FileExists(localPath))
            {
                foreach (int id in await televisionRepository.FlagUnavailable(library, incoming))
                {
                    await _searchIndex.RebuildItems(_searchRepository, new List<int> { id });
                }
            }

            return false;
        }

        if (maybeExisting.IsNone)
        {
            _logger.LogDebug(
                "INSERT: new episode {Show} s{SeasonNumber:00}e{EpisodeNumber:00}",
                show.ShowMetadata.Head().Title,
                season.SeasonNumber,
                incoming.EpisodeMetadata.Head().EpisodeNumber);
        }
        else
        {
            _logger.LogDebug(
                "UPDATE: Etag has changed for episode {Show} s{SeasonNumber:00}e{EpisodeNumber:00}",
                show.ShowMetadata.Head().Title,
                season.SeasonNumber,
                incoming.EpisodeMetadata.Head().EpisodeNumber);
        }

        return true;
    }

    private async Task<Either<BaseError, MediaItemScanResult<TShow>>> UpdateMetadata(
        TConnectionParameters connectionParameters,
        TLibrary library,
        MediaItemScanResult<TShow> result,
        TShow incoming,
        bool deepScan)
    {
        foreach (ShowMetadata fullMetadata in await GetFullMetadata(
                     connectionParameters,
                     library,
                     result,
                     incoming,
                     deepScan))
        {
            // TODO: move some of this code into this scanner
            // will have to merge JF, Emby, Plex logic
            return await UpdateMetadata(result, fullMetadata);
        }

        return result;
    }

    private async Task<Either<BaseError, MediaItemScanResult<TSeason>>> UpdateMetadata(
        TConnectionParameters connectionParameters,
        TLibrary library,
        MediaItemScanResult<TSeason> result,
        TSeason incoming,
        bool deepScan)
    {
        foreach (SeasonMetadata fullMetadata in await GetFullMetadata(
                     connectionParameters,
                     library,
                     result,
                     incoming,
                     deepScan))
        {
            // TODO: move some of this code into this scanner
            // will have to merge JF, Emby, Plex logic
            return await UpdateMetadata(result, fullMetadata);
        }

        return result;
    }

    private async Task<Either<BaseError, MediaItemScanResult<TEpisode>>> UpdateMetadata(
        TConnectionParameters connectionParameters,
        TLibrary library,
        MediaItemScanResult<TEpisode> result,
        TEpisode incoming,
        bool deepScan)
    {
        foreach (EpisodeMetadata fullMetadata in await GetFullMetadata(
                     connectionParameters,
                     library,
                     result,
                     incoming,
                     deepScan))
        {
            // TODO: move some of this code into this scanner
            // will have to merge JF, Emby, Plex logic
            return await UpdateMetadata(result, fullMetadata);
        }

        return result;
    }

    private async Task<Either<BaseError, MediaItemScanResult<TEpisode>>> UpdateStatistics(
        MediaItemScanResult<TEpisode> result,
        TEpisode incoming,
        string ffmpegPath,
        string ffprobePath)
    {
        TEpisode existing = result.Item;

        if (result.IsAdded || MediaServerItemId(existing) != MediaServerItemId(incoming) ||
            existing.MediaVersions.Head().Streams.Count == 0)
        {
            if (_localFileSystem.FileExists(result.LocalPath))
            {
                _logger.LogDebug("Refreshing {Attribute} for {Path}", "Statistics", result.LocalPath);
                Either<BaseError, bool> refreshResult =
                    await _localStatisticsProvider.RefreshStatistics(
                        ffmpegPath,
                        ffprobePath,
                        existing,
                        result.LocalPath);

                foreach (BaseError error in refreshResult.LeftToSeq())
                {
                    _logger.LogWarning(
                        "Unable to refresh {Attribute} for media item {Path}. Error: {Error}",
                        "Statistics",
                        result.LocalPath,
                        error.Value);
                }

                foreach (bool _ in refreshResult.RightToSeq())
                {
                    result.IsUpdated = true;
                }
            }
        }

        return result;
    }

    private async Task<Either<BaseError, MediaItemScanResult<TEpisode>>> UpdateSubtitles(
        MediaItemScanResult<TEpisode> existing)
    {
        try
        {
            // skip checking subtitles for files that don't exist locally
            if (!_localFileSystem.FileExists(existing.LocalPath))
            {
                return existing;
            }

            if (await _localSubtitlesProvider.UpdateSubtitles(existing.Item, existing.LocalPath, false))
            {
                return existing;
            }

            return BaseError.New("Failed to update local subtitles");
        }
        catch (Exception ex)
        {
            return BaseError.New(ex.ToString());
        }
    }
}
