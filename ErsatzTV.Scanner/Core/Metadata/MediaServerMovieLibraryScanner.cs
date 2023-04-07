using System.Collections.Immutable;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.MediaServer;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.MediaSources;
using ErsatzTV.Core.Metadata;
using ErsatzTV.Scanner.Core.Interfaces.Metadata;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Scanner.Core.Metadata;

public abstract class MediaServerMovieLibraryScanner<TConnectionParameters, TLibrary, TMovie, TEtag>
    where TConnectionParameters : MediaServerConnectionParameters
    where TLibrary : Library
    where TMovie : Movie
    where TEtag : MediaServerItemEtag
{
    private readonly ILocalFileSystem _localFileSystem;
    private readonly IMetadataRepository _metadataRepository;
    private readonly ILocalSubtitlesProvider _localSubtitlesProvider;
    private readonly ILogger _logger;
    private readonly IMediator _mediator;

    protected MediaServerMovieLibraryScanner(
        ILocalSubtitlesProvider localSubtitlesProvider,
        ILocalFileSystem localFileSystem,
        IMetadataRepository metadataRepository,
        IMediator mediator,
        ILogger logger)
    {
        _localSubtitlesProvider = localSubtitlesProvider;
        _localFileSystem = localFileSystem;
        _metadataRepository = metadataRepository;
        _mediator = mediator;
        _logger = logger;
    }
    
    protected virtual bool ServerSupportsRemoteStreaming => false;
    protected virtual bool ServerReturnsStatisticsWithMetadata => false;

    protected async Task<Either<BaseError, Unit>> ScanLibrary(
        IMediaServerMovieRepository<TLibrary, TMovie, TEtag> movieRepository,
        TConnectionParameters connectionParameters,
        TLibrary library,
        Func<TMovie, string> getLocalPath,
        string ffmpegPath,
        string ffprobePath,
        bool deepScan,
        CancellationToken cancellationToken)
    {
        try
        {
            Either<BaseError, int> maybeCount = await CountMovieLibraryItems(connectionParameters, library);
            foreach (BaseError error in maybeCount.LeftToSeq())
            {
                return error;
            }

            foreach (int count in maybeCount.RightToSeq())
            {
                return await ScanLibrary(
                    movieRepository,
                    connectionParameters,
                    library,
                    getLocalPath,
                    ffmpegPath,
                    ffprobePath,
                    GetMovieLibraryItems(connectionParameters, library),
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

    private async Task<Either<BaseError, Unit>> ScanLibrary(
        IMediaServerMovieRepository<TLibrary, TMovie, TEtag> movieRepository,
        TConnectionParameters connectionParameters,
        TLibrary library,
        Func<TMovie, string> getLocalPath,
        string ffmpegPath,
        string ffprobePath,
        IAsyncEnumerable<TMovie> movieEntries,
        int totalMovieCount,
        bool deepScan,
        CancellationToken cancellationToken)
    {
        var incomingItemIds = new List<string>();
        IReadOnlyDictionary<string, TEtag> existingMovies = (await movieRepository.GetExistingMovies(library))
            .ToImmutableDictionary(e => e.MediaServerItemId, e => e);

        await foreach (TMovie incoming in movieEntries.WithCancellation(cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new ScanCanceled();
            }

            incomingItemIds.Add(MediaServerItemId(incoming));

            decimal percentCompletion = Math.Clamp((decimal)incomingItemIds.Count / totalMovieCount, 0, 1);
            await _mediator.Publish(
                new ScannerProgressUpdate(
                    library.Id,
                    library.Name,
                    percentCompletion,
                    Array.Empty<int>(),
                    Array.Empty<int>()),
                cancellationToken);

            string localPath = getLocalPath(incoming);

            if (await ShouldScanItem(movieRepository, library, existingMovies, incoming, localPath, deepScan) == false)
            {
                continue;
            }
            
            Either<BaseError, MediaItemScanResult<TMovie>> maybeMovie;

            if (ServerReturnsStatisticsWithMetadata)
            {
                maybeMovie = await movieRepository
                    .GetOrAdd(library, incoming, deepScan)
                    .MapT(
                        result =>
                        {
                            result.LocalPath = localPath;
                            return result;
                        })
                    .BindT(
                        existing => UpdateMetadataAndStatistics(
                            connectionParameters,
                            library,
                            existing,
                            incoming,
                            deepScan))
                    .BindT(UpdateSubtitles);
            }
            else
            {
                maybeMovie = await movieRepository
                    .GetOrAdd(library, incoming, deepScan)
                    .MapT(
                        result =>
                        {
                            result.LocalPath = localPath;
                            return result;
                        })
                    .BindT(
                        existing => UpdateMetadata(connectionParameters, library, existing, incoming, deepScan, None))
                    .BindT(
                        existing => UpdateStatistics(
                            connectionParameters,
                            library,
                            existing,
                            incoming,
                            deepScan,
                            ffmpegPath,
                            ffprobePath,
                            None))
                    .BindT(UpdateSubtitles);
            }

            if (maybeMovie.IsLeft)
            {
                foreach (BaseError error in maybeMovie.LeftToSeq())
                {
                    _logger.LogWarning(
                        "Error processing movie {Title}: {Error}",
                        incoming.MovieMetadata.Head().Title,
                        error.Value);
                }

                continue;
            }

            foreach (MediaItemScanResult<TMovie> result in maybeMovie.RightToSeq())
            {
                await movieRepository.SetEtag(result.Item, MediaServerEtag(incoming));

                if (_localFileSystem.FileExists(result.LocalPath))
                {
                    if (await movieRepository.FlagNormal(library, result.Item))
                    {
                        result.IsUpdated = true;
                    }
                }
                else if (ServerSupportsRemoteStreaming)
                {
                    Option<int> flagResult = await movieRepository.FlagRemoteOnly(library, result.Item);
                    if (flagResult.IsSome)
                    {
                        result.IsUpdated = true;
                    }
                }
                else
                {
                    Option<int> flagResult = await movieRepository.FlagUnavailable(library, result.Item);
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

        // trash movies that are no longer present on the media server
        var fileNotFoundItemIds = existingMovies.Keys.Except(incomingItemIds).ToList();
        List<int> ids = await movieRepository.FlagFileNotFound(library, fileNotFoundItemIds);
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

    protected abstract string MediaServerItemId(TMovie movie);
    protected abstract string MediaServerEtag(TMovie movie);

    protected abstract Task<Either<BaseError, int>> CountMovieLibraryItems(
        TConnectionParameters connectionParameters,
        TLibrary library);

    protected abstract IAsyncEnumerable<TMovie> GetMovieLibraryItems(
        TConnectionParameters connectionParameters,
        TLibrary library);

    protected abstract Task<Option<MovieMetadata>> GetFullMetadata(
        TConnectionParameters connectionParameters,
        TLibrary library,
        MediaItemScanResult<TMovie> result,
        TMovie incoming,
        bool deepScan);

    protected virtual Task<Option<MediaVersion>> GetMediaServerStatistics(
        TConnectionParameters connectionParameters,
        TLibrary library,
        MediaItemScanResult<TMovie> result,
        TMovie incoming) => Task.FromResult(Option<MediaVersion>.None);
    
    protected abstract Task<Option<Tuple<MovieMetadata, MediaVersion>>> GetFullMetadataAndStatistics(
        TConnectionParameters connectionParameters,
        TLibrary library,
        MediaItemScanResult<TMovie> result,
        TMovie incoming);

    protected abstract Task<Either<BaseError, MediaItemScanResult<TMovie>>> UpdateMetadata(
        MediaItemScanResult<TMovie> result,
        MovieMetadata fullMetadata);

    private async Task<bool> ShouldScanItem(
        IMediaServerMovieRepository<TLibrary, TMovie, TEtag> movieRepository,
        TLibrary library,
        IReadOnlyDictionary<string, TEtag> existingMovies,
        TMovie incoming,
        string localPath,
        bool deepScan)
    {
        // deep scan will always pull every movie
        if (deepScan)
        {
            return true;
        }

        string existingEtag = string.Empty;
        MediaItemState existingState = MediaItemState.Normal;
        if (existingMovies.TryGetValue(MediaServerItemId(incoming), out TEtag? existingEntry))
        {
            existingEtag = existingEntry.Etag;
            existingState = existingEntry.State;
        }

        if (existingState is MediaItemState.Unavailable or MediaItemState.FileNotFound &&
            existingEtag == MediaServerEtag(incoming))
        {
            // skip scanning unavailable/file not found items that are unchanged and still don't exist locally
            if (!_localFileSystem.FileExists(localPath) && !ServerSupportsRemoteStreaming)
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
                if (ServerSupportsRemoteStreaming)
                {
                    foreach (int id in await movieRepository.FlagRemoteOnly(library, incoming))
                    {
                        await _mediator.Publish(
                            new ScannerProgressUpdate(library.Id, null, null, new[] { id }, Array.Empty<int>()),
                            CancellationToken.None);
                    }
                }
                else
                {
                    foreach (int id in await movieRepository.FlagUnavailable(library, incoming))
                    {
                        await _mediator.Publish(
                            new ScannerProgressUpdate(library.Id, null, null, new[] { id }, Array.Empty<int>()),
                            CancellationToken.None);
                    }
                }
            }

            return false;
        }

        if (existingEntry is null)
        {
            _logger.LogDebug("INSERT: new movie {Movie}", incoming.MovieMetadata.Head().Title);
        }
        else
        {
            _logger.LogDebug("UPDATE: Etag has changed for movie {Movie}", incoming.MovieMetadata.Head().Title);
        }

        return true;
    }

    private async Task<Either<BaseError, MediaItemScanResult<TMovie>>> UpdateMetadataAndStatistics(
        TConnectionParameters connectionParameters,
        TLibrary library,
        MediaItemScanResult<TMovie> result,
        TMovie incoming,
        bool deepScan)
    {
        Option<Tuple<MovieMetadata, MediaVersion>> maybeMetadataAndStatistics = await GetFullMetadataAndStatistics(
            connectionParameters,
            library,
            result,
            incoming);

        foreach ((MovieMetadata fullMetadata, MediaVersion mediaVersion) in maybeMetadataAndStatistics)
        {
            Either<BaseError, MediaItemScanResult<TMovie>> metadataResult = await UpdateMetadata(
                connectionParameters,
                library,
                result,
                incoming,
                deepScan,
                fullMetadata);

            foreach (BaseError error in metadataResult.LeftToSeq())
            {
                return error;
            }

            foreach (MediaItemScanResult<TMovie> r in metadataResult.RightToSeq())
            {
                result = r;
            }

            Either<BaseError, MediaItemScanResult<TMovie>> statisticsResult = await UpdateStatistics(
                connectionParameters,
                library,
                result,
                incoming,
                deepScan,
                string.Empty,
                string.Empty,
                mediaVersion);
            
            foreach (BaseError error in statisticsResult.LeftToSeq())
            {
                return error;
            }
            
            foreach (MediaItemScanResult<TMovie> r in metadataResult.RightToSeq())
            {
                result = r;
            }
        }

        return result;
    }
    
    private async Task<Either<BaseError, MediaItemScanResult<TMovie>>> UpdateMetadata(
        TConnectionParameters connectionParameters,
        TLibrary library,
        MediaItemScanResult<TMovie> result,
        TMovie incoming,
        bool deepScan,
        Option<MovieMetadata> maybeFullMetadata)
    {
        if (maybeFullMetadata.IsNone)
        {
            maybeFullMetadata = await GetFullMetadata(connectionParameters, library, result, incoming, deepScan);
        }

        foreach (MovieMetadata fullMetadata in maybeFullMetadata)
        {
            // TODO: move some of this code into this scanner
            // will have to merge JF, Emby, Plex logic
            return await UpdateMetadata(result, fullMetadata);
        }

        return result;
    }

    private async Task<Either<BaseError, MediaItemScanResult<TMovie>>> UpdateStatistics(
        TConnectionParameters connectionParameters,
        TLibrary library,
        MediaItemScanResult<TMovie> result,
        TMovie incoming,
        bool deepScan,
        string ffmpegPath,
        string ffprobePath,
        Option<MediaVersion> maybeMediaVersion)
    {
        TMovie existing = result.Item;

        if (deepScan || result.IsAdded || MediaServerEtag(existing) != MediaServerEtag(incoming) ||
            existing.MediaVersions.Head().Streams.Count == 0)
        {
            if (maybeMediaVersion.IsNone)
            {
                maybeMediaVersion = await GetMediaServerStatistics(
                    connectionParameters,
                    library,
                    result,
                    incoming);
            }

            foreach (MediaVersion mediaVersion in maybeMediaVersion)
            {
                if (await _metadataRepository.UpdateStatistics(result.Item, mediaVersion))
                {
                    result.IsUpdated = true;
                }
            }
        }

        return result;
    }

    private async Task<Either<BaseError, MediaItemScanResult<TMovie>>> UpdateSubtitles(
        MediaItemScanResult<TMovie> existing)
    {
        try
        {
            Option<MovieMetadata> maybeMetadata = existing.Item.MovieMetadata.HeadOrNone();
            foreach (MovieMetadata metadata in maybeMetadata)
            {
                var subtitleStreams = existing.Item.GetHeadVersion().Streams
                    .Filter(s => s.MediaStreamKind == MediaStreamKind.Subtitle)
                    .ToList();

                var subtitles = subtitleStreams.Map(Subtitle.FromMediaStream).ToList();
                
                // TODO: check for/add external subtitles

                if (await _metadataRepository.UpdateSubtitles(metadata, subtitles))
                {
                    return existing;
                }

                // skip checking subtitles for files that don't exist locally
                // if (!_localFileSystem.FileExists(existing.LocalPath))
                // {
                // return existing;
                // }
                //
                // if (await _localSubtitlesProvider.UpdateSubtitles(existing.Item, existing.LocalPath, false))
                // {
                //     return existing;
                // }
            }

            return BaseError.New("Failed to update local subtitles");
        }
        catch (Exception ex)
        {
            return BaseError.New(ex.ToString());
        }
    }
}
