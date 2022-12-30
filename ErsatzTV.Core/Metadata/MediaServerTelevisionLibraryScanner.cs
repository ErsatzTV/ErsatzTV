using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.MediaServer;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.MediaSources;
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

    protected MediaServerTelevisionLibraryScanner(
        ILocalStatisticsProvider localStatisticsProvider,
        ILocalSubtitlesProvider localSubtitlesProvider,
        ILocalFileSystem localFileSystem,
        IMediator mediator,
        ILogger logger)
    {
        _localStatisticsProvider = localStatisticsProvider;
        _localSubtitlesProvider = localSubtitlesProvider;
        _localFileSystem = localFileSystem;
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
            Either<BaseError, int> maybeCount = await CountShowLibraryItems(connectionParameters, library);
            foreach (BaseError error in maybeCount.LeftToSeq())
            {
                return error;
            }

            foreach (int count in maybeCount.RightToSeq())
            {
                _logger.LogDebug("Library {Library} contains {Count} shows", library.Name, count);

                return await ScanLibrary(
                    televisionRepository,
                    connectionParameters,
                    library,
                    getLocalPath,
                    ffmpegPath,
                    ffprobePath,
                    GetShowLibraryItems(connectionParameters, library),
                    count,
                    deepScan,
                    cancellationToken);
            }

            // this won't happen
            return Unit.Default;
        }
        catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
        {
            return new ScanCanceled();
        }
    }

    protected abstract Task<Either<BaseError, int>> CountShowLibraryItems(
        TConnectionParameters connectionParameters,
        TLibrary library);

    protected abstract IAsyncEnumerable<TShow> GetShowLibraryItems(
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
        IAsyncEnumerable<TShow> showEntries,
        int totalShowCount,
        bool deepScan,
        CancellationToken cancellationToken)
    {
        var incomingItemIds = new List<string>();
        List<TEtag> existingShows = await televisionRepository.GetExistingShows(library);

        await foreach (TShow incoming in showEntries.WithCancellation(cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new ScanCanceled();
            }

            incomingItemIds.Add(MediaServerItemId(incoming));

            decimal percentCompletion = Math.Clamp((decimal)incomingItemIds.Count / totalShowCount, 0, 1);
            await _mediator.Publish(
                new ScannerProgressUpdate(
                    library.Id,
                    library.Name,
                    percentCompletion,
                    Array.Empty<int>(),
                    Array.Empty<int>()),
                cancellationToken);

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
                Either<BaseError, int> maybeCount = await CountSeasonLibraryItems(
                    connectionParameters,
                    library,
                    result.Item);
                foreach (BaseError error in maybeCount.LeftToSeq())
                {
                    return error;
                }

                foreach (int count in maybeCount.RightToSeq())
                {
                    _logger.LogDebug(
                        "Show {Title} contains {Count} seasons",
                        result.Item.ShowMetadata.Head().Title,
                        count);
                }

                Either<BaseError, Unit> scanResult = await ScanSeasons(
                    televisionRepository,
                    library,
                    getLocalPath,
                    result.Item,
                    connectionParameters,
                    ffmpegPath,
                    ffprobePath,
                    GetSeasonLibraryItems(library, connectionParameters, result.Item),
                    deepScan,
                    cancellationToken);

                foreach (ScanCanceled error in scanResult.LeftToSeq().OfType<ScanCanceled>())
                {
                    return error;
                }

                await televisionRepository.SetEtag(result.Item, MediaServerEtag(incoming));

                if (await televisionRepository.FlagNormal(library, result.Item))
                {
                    result.IsUpdated = true;
                }

                if (result.IsAdded || result.IsUpdated)
                {
                    await _mediator.Publish(
                        new ScannerProgressUpdate(
                            library.Id,
                            null,
                            null,
                            new[] { result.Item.Id },
                            Array.Empty<int>()),
                        cancellationToken);
                }
            }
        }

        // trash shows that are no longer present on the media server
        var fileNotFoundItemIds = existingShows.Map(s => s.MediaServerItemId).Except(incomingItemIds).ToList();
        List<int> ids = await televisionRepository.FlagFileNotFoundShows(library, fileNotFoundItemIds);
        await _mediator.Publish(
            new ScannerProgressUpdate(library.Id, null, null, ids.ToArray(), Array.Empty<int>()),
            cancellationToken);

        await _mediator.Publish(
            new ScannerProgressUpdate(
                library.Id,
                library.Name,
                0,
                Array.Empty<int>(),
                Array.Empty<int>()),
            cancellationToken);

        return Unit.Default;
    }

    protected abstract Task<Either<BaseError, int>> CountSeasonLibraryItems(
        TConnectionParameters connectionParameters,
        TLibrary library,
        TShow show);

    protected abstract IAsyncEnumerable<TSeason> GetSeasonLibraryItems(
        TLibrary library,
        TConnectionParameters connectionParameters,
        TShow show);

    protected abstract Task<Either<BaseError, int>> CountEpisodeLibraryItems(
        TConnectionParameters connectionParameters,
        TLibrary library,
        TSeason season);

    protected abstract IAsyncEnumerable<TEpisode> GetEpisodeLibraryItems(
        TLibrary library,
        TConnectionParameters connectionParameters,
        TShow show,
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
        IAsyncEnumerable<TSeason> seasonEntries,
        bool deepScan,
        CancellationToken cancellationToken)
    {
        var incomingItemIds = new List<string>();
        List<TEtag> existingSeasons = await televisionRepository.GetExistingSeasons(library, show);

        await foreach (TSeason incoming in seasonEntries.WithCancellation(cancellationToken))
        {
            incoming.ShowId = show.Id;

            if (cancellationToken.IsCancellationRequested)
            {
                return new ScanCanceled();
            }

            incomingItemIds.Add(MediaServerItemId(incoming));

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
                Either<BaseError, int> maybeCount = await CountEpisodeLibraryItems(
                    connectionParameters,
                    library,
                    result.Item);
                foreach (BaseError error in maybeCount.LeftToSeq())
                {
                    return error;
                }

                foreach (int count in maybeCount.RightToSeq())
                {
                    _logger.LogDebug(
                        "Show {Title} season {Season} contains {Count} episodes",
                        show.ShowMetadata.Head().Title,
                        result.Item.SeasonNumber,
                        count);
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
                    GetEpisodeLibraryItems(library, connectionParameters, show, result.Item),
                    deepScan,
                    cancellationToken);

                foreach (ScanCanceled error in scanResult.LeftToSeq().OfType<ScanCanceled>())
                {
                    return error;
                }

                await televisionRepository.SetEtag(result.Item, MediaServerEtag(incoming));

                if (await televisionRepository.FlagNormal(library, result.Item))
                {
                    result.IsUpdated = true;
                }

                result.Item.Show = show;

                if (result.IsAdded || result.IsUpdated)
                {
                    await _mediator.Publish(
                        new ScannerProgressUpdate(
                            library.Id,
                            null,
                            null,
                            new[] { result.Item.Id },
                            Array.Empty<int>()),
                        cancellationToken);
                }
            }
        }

        // trash seasons that are no longer present on the media server
        var fileNotFoundItemIds = existingSeasons.Map(s => s.MediaServerItemId).Except(incomingItemIds).ToList();
        List<int> ids = await televisionRepository.FlagFileNotFoundSeasons(library, fileNotFoundItemIds);
        await _mediator.Publish(
            new ScannerProgressUpdate(library.Id, null, null, ids.ToArray(), Array.Empty<int>()),
            cancellationToken);

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
        IAsyncEnumerable<TEpisode> episodeEntries,
        bool deepScan,
        CancellationToken cancellationToken)
    {
        var incomingItemIds = new List<string>();
        List<TEtag> existingEpisodes = await televisionRepository.GetExistingEpisodes(library, season);

        await foreach (TEpisode incoming in episodeEntries.WithCancellation(cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new ScanCanceled();
            }

            incomingItemIds.Add(MediaServerItemId(incoming));

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

                if (result.IsAdded || result.IsUpdated)
                {
                    await _mediator.Publish(
                        new ScannerProgressUpdate(
                            library.Id,
                            null,
                            null,
                            new[] { result.Item.Id },
                            Array.Empty<int>()),
                        cancellationToken);
                }
            }
        }

        // trash episodes that are no longer present on the media server
        var fileNotFoundItemIds = existingEpisodes.Map(m => m.MediaServerItemId).Except(incomingItemIds).ToList();
        List<int> ids = await televisionRepository.FlagFileNotFoundEpisodes(library, fileNotFoundItemIds);
        await _mediator.Publish(
            new ScannerProgressUpdate(library.Id, null, null, ids.ToArray(), Array.Empty<int>()),
            cancellationToken);

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
        string existingEtag = await maybeExisting.Map(e => e.Etag ?? string.Empty).IfNoneAsync(string.Empty);
        MediaItemState existingState = await maybeExisting.Map(e => e.State).IfNoneAsync(MediaItemState.Normal);

        if (existingState is MediaItemState.Unavailable or MediaItemState.FileNotFound &&
            existingEtag == MediaServerEtag(incoming))
        {
            // skip scanning unavailable/file not found items that are unchanged and still don't exist locally
            if (!_localFileSystem.FileExists(localPath))
            {
                return false;
            }
        }
        else if (existingEtag == MediaServerEtag(incoming))
        {
            // item is unchanged, but file does not exist
            // don't scan, but mark as unavailable
            if (!_localFileSystem.FileExists(localPath))
            {
                foreach (int id in await televisionRepository.FlagUnavailable(library, incoming))
                {
                    await _mediator.Publish(
                        new ScannerProgressUpdate(library.Id, null, null, new[] { id }, Array.Empty<int>()),
                        CancellationToken.None);
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

        if (result.IsAdded || MediaServerEtag(existing) != MediaServerEtag(incoming) ||
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
