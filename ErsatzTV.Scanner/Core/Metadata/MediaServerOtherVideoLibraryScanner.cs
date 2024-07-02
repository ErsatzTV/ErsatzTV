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
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Scanner.Core.Metadata;

public abstract class MediaServerOtherVideoLibraryScanner<TConnectionParameters, TLibrary, TOtherVideo, TEtag>
    where TConnectionParameters : MediaServerConnectionParameters
    where TLibrary : Library
    where TOtherVideo : OtherVideo
    where TEtag : MediaServerItemEtag
{
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILogger _logger;
    private readonly IMediator _mediator;
    private readonly IMetadataRepository _metadataRepository;

    protected MediaServerOtherVideoLibraryScanner(
        ILocalFileSystem localFileSystem,
        IMetadataRepository metadataRepository,
        IMediator mediator,
        ILogger logger)
    {
        _localFileSystem = localFileSystem;
        _metadataRepository = metadataRepository;
        _mediator = mediator;
        _logger = logger;
    }

    protected virtual bool ServerSupportsRemoteStreaming => false;
    protected virtual bool ServerReturnsStatisticsWithMetadata => false;

    protected async Task<Either<BaseError, Unit>> ScanLibrary(
        IMediaServerOtherVideoRepository<TLibrary, TOtherVideo, TEtag> otherVideoRepository,
        TConnectionParameters connectionParameters,
        TLibrary library,
        Func<TOtherVideo, string> getLocalPath,
        bool deepScan,
        CancellationToken cancellationToken)
    {
        try
        {
            return await ScanLibrary(
                otherVideoRepository,
                connectionParameters,
                library,
                getLocalPath,
                GetOtherVideoLibraryItems(connectionParameters, library),
                deepScan,
                cancellationToken);
        }
        catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
        {
            return new ScanCanceled();
        }
    }

    private async Task<Either<BaseError, Unit>> ScanLibrary(
        IMediaServerOtherVideoRepository<TLibrary, TOtherVideo, TEtag> otherVideoRepository,
        TConnectionParameters connectionParameters,
        TLibrary library,
        Func<TOtherVideo, string> getLocalPath,
        IAsyncEnumerable<Tuple<TOtherVideo, int>> otherVideoEntries,
        bool deepScan,
        CancellationToken cancellationToken)
    {
        var incomingItemIds = new List<string>();
        IReadOnlyDictionary<string, TEtag> existingOtherVideos = (await otherVideoRepository.GetExistingOtherVideos(library))
            .ToImmutableDictionary(e => e.MediaServerItemId, e => e);

        await foreach ((TOtherVideo incoming, int totalOtherVideoCount) in otherVideoEntries.WithCancellation(cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new ScanCanceled();
            }

            incomingItemIds.Add(MediaServerItemId(incoming));

            decimal percentCompletion = Math.Clamp((decimal)incomingItemIds.Count / totalOtherVideoCount, 0, 1);
            await _mediator.Publish(
                new ScannerProgressUpdate(
                    library.Id,
                    library.Name,
                    percentCompletion,
                    Array.Empty<int>(),
                    Array.Empty<int>()),
                cancellationToken);

            string localPath = getLocalPath(incoming);

            if (await ShouldScanItem(otherVideoRepository, library, existingOtherVideos, incoming, localPath, deepScan) == false)
            {
                continue;
            }

            Either<BaseError, MediaItemScanResult<TOtherVideo>> maybeOtherVideo;

            if (ServerReturnsStatisticsWithMetadata)
            {
                maybeOtherVideo = await otherVideoRepository
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
                            deepScan));
            }
            else
            {
                maybeOtherVideo = await otherVideoRepository
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
                            None))
                    .BindT(UpdateSubtitles);
            }

            if (maybeOtherVideo.IsLeft)
            {
                foreach (BaseError error in maybeOtherVideo.LeftToSeq())
                {
                    _logger.LogWarning(
                        "Error processing other video {Title}: {Error}",
                        incoming.OtherVideoMetadata.Head().Title,
                        error.Value);
                }

                continue;
            }

            foreach (MediaItemScanResult<TOtherVideo> result in maybeOtherVideo.RightToSeq())
            {
                await otherVideoRepository.SetEtag(result.Item, MediaServerEtag(incoming));

                if (_localFileSystem.FileExists(result.LocalPath))
                {
                    Option<int> flagResult = await otherVideoRepository.FlagNormal(library, result.Item);
                    if (flagResult.IsSome)
                    {
                        result.IsUpdated = true;
                    }
                }
                else if (ServerSupportsRemoteStreaming)
                {
                    Option<int> flagResult = await otherVideoRepository.FlagRemoteOnly(library, result.Item);
                    if (flagResult.IsSome)
                    {
                        result.IsUpdated = true;
                    }
                }
                else
                {
                    Option<int> flagResult = await otherVideoRepository.FlagUnavailable(library, result.Item);
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

        // trash OtherVideo that are no longer present on the media server
        var fileNotFoundItemIds = existingOtherVideos.Keys.Except(incomingItemIds).ToList();
        List<int> ids = await otherVideoRepository.FlagFileNotFound(library, fileNotFoundItemIds);
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

    protected abstract string MediaServerItemId(TOtherVideo otherVideo);
    protected abstract string MediaServerEtag(TOtherVideo otherVideo);

    protected abstract IAsyncEnumerable<Tuple<TOtherVideo, int>> GetOtherVideoLibraryItems(
        TConnectionParameters connectionParameters,
        TLibrary library);

    protected abstract Task<Option<OtherVideoMetadata>> GetFullMetadata(
        TConnectionParameters connectionParameters,
        TLibrary library,
        MediaItemScanResult<TOtherVideo> result,
        TOtherVideo incoming,
        bool deepScan);

    protected virtual Task<Option<MediaVersion>> GetMediaServerStatistics(
        TConnectionParameters connectionParameters,
        TLibrary library,
        MediaItemScanResult<TOtherVideo> result,
        TOtherVideo incoming) => Task.FromResult(Option<MediaVersion>.None);

    protected abstract Task<Option<Tuple<OtherVideoMetadata, MediaVersion>>> GetFullMetadataAndStatistics(
        TConnectionParameters connectionParameters,
        TLibrary library,
        MediaItemScanResult<TOtherVideo> result,
        TOtherVideo incoming);

    protected abstract Task<Either<BaseError, MediaItemScanResult<TOtherVideo>>> UpdateMetadata(
        MediaItemScanResult<TOtherVideo> result,
        OtherVideoMetadata fullMetadata);

    private async Task<bool> ShouldScanItem(
        IMediaServerOtherVideoRepository<TLibrary, TOtherVideo, TEtag> otherVideoRepository,
        TLibrary library,
        IReadOnlyDictionary<string, TEtag> existingOtherVideos,
        TOtherVideo incoming,
        string localPath,
        bool deepScan)
    {
        // deep scan will always pull every OtherVideo
        if (deepScan)
        {
            return true;
        }

        string existingEtag = string.Empty;
        MediaItemState existingState = MediaItemState.Normal;
        if (existingOtherVideos.TryGetValue(MediaServerItemId(incoming), out TEtag? existingEntry))
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
                    if (existingState is not MediaItemState.RemoteOnly)
                    {
                        foreach (int id in await otherVideoRepository.FlagRemoteOnly(library, incoming))
                        {
                            await _mediator.Publish(
                                new ScannerProgressUpdate(library.Id, null, null, new[] { id }, Array.Empty<int>()),
                                CancellationToken.None);
                        }
                    }
                }
                else
                {
                    if (existingState is not MediaItemState.Unavailable)
                    {
                        foreach (int id in await otherVideoRepository.FlagUnavailable(library, incoming))
                        {
                            await _mediator.Publish(
                                new ScannerProgressUpdate(library.Id, null, null, new[] { id }, Array.Empty<int>()),
                                CancellationToken.None);
                        }
                    }
                }
            }

            return false;
        }

        if (existingEntry is null)
        {
            _logger.LogDebug("INSERT: new other video {OtherVideo}", incoming.OtherVideoMetadata.Head().Title);
        }
        else
        {
            _logger.LogDebug("UPDATE: Etag has changed for other video {OtherVideo}", incoming.OtherVideoMetadata.Head().Title);
        }

        return true;
    }

    private async Task<Either<BaseError, MediaItemScanResult<TOtherVideo>>> UpdateMetadataAndStatistics(
        TConnectionParameters connectionParameters,
        TLibrary library,
        MediaItemScanResult<TOtherVideo> result,
        TOtherVideo incoming,
        bool deepScan)
    {
        Option<Tuple<OtherVideoMetadata, MediaVersion>> maybeMetadataAndStatistics = await GetFullMetadataAndStatistics(
            connectionParameters,
            library,
            result,
            incoming);

        foreach ((OtherVideoMetadata fullMetadata, MediaVersion mediaVersion) in maybeMetadataAndStatistics)
        {
            Either<BaseError, MediaItemScanResult<TOtherVideo>> metadataResult = await UpdateMetadata(
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

            foreach (MediaItemScanResult<TOtherVideo> r in metadataResult.RightToSeq())
            {
                result = r;
            }

            Either<BaseError, MediaItemScanResult<TOtherVideo>> statisticsResult = await UpdateStatistics(
                connectionParameters,
                library,
                result,
                incoming,
                deepScan,
                mediaVersion);

            foreach (BaseError error in statisticsResult.LeftToSeq())
            {
                return error;
            }

            foreach (MediaItemScanResult<TOtherVideo> r in metadataResult.RightToSeq())
            {
                result = r;
            }
        }

        return result;
    }

    private async Task<Either<BaseError, MediaItemScanResult<TOtherVideo>>> UpdateMetadata(
        TConnectionParameters connectionParameters,
        TLibrary library,
        MediaItemScanResult<TOtherVideo> result,
        TOtherVideo incoming,
        bool deepScan,
        Option<OtherVideoMetadata> maybeFullMetadata)
    {
        if (maybeFullMetadata.IsNone)
        {
            maybeFullMetadata = await GetFullMetadata(connectionParameters, library, result, incoming, deepScan);
        }

        foreach (OtherVideoMetadata fullMetadata in maybeFullMetadata)
        {
            // TODO: move some of this code into this scanner
            // will have to merge JF, Emby, Plex logic
            return await UpdateMetadata(result, fullMetadata);
        }

        return result;
    }

    private async Task<Either<BaseError, MediaItemScanResult<TOtherVideo>>> UpdateStatistics(
        TConnectionParameters connectionParameters,
        TLibrary library,
        MediaItemScanResult<TOtherVideo> result,
        TOtherVideo incoming,
        bool deepScan,
        Option<MediaVersion> maybeMediaVersion)
    {
        TOtherVideo existing = result.Item;

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


    private async Task<Either<BaseError, MediaItemScanResult<TOtherVideo>>> UpdateSubtitles(
        MediaItemScanResult<TOtherVideo> existing)
    {
        try
        {
            MediaVersion version = existing.Item.GetHeadVersion();
            Option<OtherVideoMetadata> maybeMetadata = existing.Item.OtherVideoMetadata.HeadOrNone();
            foreach (OtherVideoMetadata metadata in maybeMetadata)
            {
                List<Subtitle> subtitles = version.Streams
                    .Filter(s => s.MediaStreamKind is MediaStreamKind.Subtitle or MediaStreamKind.ExternalSubtitle)
                    .Map(Subtitle.FromMediaStream)
                    .ToList();

                if (await _metadataRepository.UpdateSubtitles(metadata, subtitles))
                {
                    return existing;
                }
            }

            return BaseError.New("Failed to update media server subtitles");
        }
        catch (Exception ex)
        {
            return BaseError.New(ex.ToString());
        }
    }
}
