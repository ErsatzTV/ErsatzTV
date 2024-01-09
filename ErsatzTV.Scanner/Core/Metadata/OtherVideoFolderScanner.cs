using Bugsnag;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Images;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.MediaSources;
using ErsatzTV.Core.Metadata;
using ErsatzTV.Scanner.Core.Interfaces.FFmpeg;
using ErsatzTV.Scanner.Core.Interfaces.Metadata;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Scanner.Core.Metadata;

public class OtherVideoFolderScanner : LocalFolderScanner, IOtherVideoFolderScanner
{
    private readonly IClient _client;
    private readonly ILibraryRepository _libraryRepository;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILocalMetadataProvider _localMetadataProvider;
    private readonly ILocalSubtitlesProvider _localSubtitlesProvider;
    private readonly ILogger<OtherVideoFolderScanner> _logger;
    private readonly IMediator _mediator;
    private readonly IOtherVideoRepository _otherVideoRepository;

    public OtherVideoFolderScanner(
        ILocalFileSystem localFileSystem,
        ILocalStatisticsProvider localStatisticsProvider,
        ILocalMetadataProvider localMetadataProvider,
        ILocalSubtitlesProvider localSubtitlesProvider,
        IMetadataRepository metadataRepository,
        IImageCache imageCache,
        IMediator mediator,
        IOtherVideoRepository otherVideoRepository,
        ILibraryRepository libraryRepository,
        IMediaItemRepository mediaItemRepository,
        IFFmpegPngService ffmpegPngService,
        ITempFilePool tempFilePool,
        IClient client,
        ILogger<OtherVideoFolderScanner> logger) : base(
        localFileSystem,
        localStatisticsProvider,
        metadataRepository,
        mediaItemRepository,
        imageCache,
        ffmpegPngService,
        tempFilePool,
        client,
        logger)
    {
        _localFileSystem = localFileSystem;
        _localMetadataProvider = localMetadataProvider;
        _localSubtitlesProvider = localSubtitlesProvider;
        _mediator = mediator;
        _otherVideoRepository = otherVideoRepository;
        _libraryRepository = libraryRepository;
        _client = client;
        _logger = logger;
    }

    public async Task<Either<BaseError, Unit>> ScanFolder(
        LibraryPath libraryPath,
        string ffmpegPath,
        string ffprobePath,
        decimal progressMin,
        decimal progressMax,
        CancellationToken cancellationToken)
    {
        try
        {
            decimal progressSpread = progressMax - progressMin;

            var foldersCompleted = 0;

            var allFolders = new System.Collections.Generic.HashSet<string>();
            var folderQueue = new Queue<string>();

            if (ShouldIncludeFolder(libraryPath.Path) && allFolders.Add(libraryPath.Path))
            {
                folderQueue.Enqueue(libraryPath.Path);
            }

            foreach (string folder in _localFileSystem.ListSubdirectories(libraryPath.Path)
                         .Filter(ShouldIncludeFolder)
                         .Filter(allFolders.Add)
                         .OrderBy(identity))
            {
                folderQueue.Enqueue(folder);
            }

            while (folderQueue.Count > 0)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return new ScanCanceled();
                }

                decimal percentCompletion = (decimal)foldersCompleted / (foldersCompleted + folderQueue.Count);
                await _mediator.Publish(
                    new ScannerProgressUpdate(
                        libraryPath.LibraryId,
                        null,
                        progressMin + percentCompletion * progressSpread,
                        Array.Empty<int>(),
                        Array.Empty<int>()),
                    cancellationToken);

                string otherVideoFolder = folderQueue.Dequeue();
                foldersCompleted++;

                var filesForEtag = _localFileSystem.ListFiles(otherVideoFolder).ToList();

                var allFiles = filesForEtag
                    .Filter(f => VideoFileExtensions.Contains(Path.GetExtension(f)))
                    .Filter(f => !Path.GetFileName(f).StartsWith("._", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (string subdirectory in _localFileSystem.ListSubdirectories(otherVideoFolder)
                             .Filter(ShouldIncludeFolder)
                             .Filter(allFolders.Add)
                             .OrderBy(identity))
                {
                    folderQueue.Enqueue(subdirectory);
                }

                string etag = FolderEtag.Calculate(otherVideoFolder, _localFileSystem);
                Option<LibraryFolder> knownFolder = libraryPath.LibraryFolders
                    .Filter(f => f.Path == otherVideoFolder)
                    .HeadOrNone();

                // skip folder if etag matches
                if (allFiles.Count == 0 ||
                    await knownFolder.Map(f => f.Etag ?? string.Empty).IfNoneAsync(string.Empty) ==
                    etag)
                {
                    continue;
                }

                _logger.LogDebug(
                    "UPDATE: Etag has changed for folder {Folder}",
                    otherVideoFolder);

                var hasErrors = false;

                foreach (string file in allFiles.OrderBy(identity))
                {
                    Either<BaseError, MediaItemScanResult<OtherVideo>> maybeVideo = await _otherVideoRepository
                        .GetOrAdd(libraryPath, file)
                        .BindT(video => UpdateStatistics(video, ffmpegPath, ffprobePath))
                        .BindT(UpdateMetadata)
                        .BindT(video => UpdateThumbnail(video, cancellationToken))
                        .BindT(UpdateSubtitles)
                        .BindT(FlagNormal);

                    foreach (BaseError error in maybeVideo.LeftToSeq())
                    {
                        _logger.LogWarning("Error processing other video at {Path}: {Error}", file, error.Value);
                        hasErrors = true;
                    }

                    foreach (MediaItemScanResult<OtherVideo> result in maybeVideo.RightToSeq())
                    {
                        if (result.IsAdded || result.IsUpdated)
                        {
                            await _mediator.Publish(
                                new ScannerProgressUpdate(
                                    libraryPath.LibraryId,
                                    null,
                                    null,
                                    [result.Item.Id],
                                    Array.Empty<int>()),
                                cancellationToken);
                        }
                    }
                }

                // only do this once per folder and only if all files processed successfully
                if (!hasErrors)
                {
                    await _libraryRepository.SetEtag(libraryPath, knownFolder, otherVideoFolder, etag);
                }
            }

            foreach (string path in await _otherVideoRepository.FindOtherVideoPaths(libraryPath))
            {
                if (!_localFileSystem.FileExists(path))
                {
                    _logger.LogInformation("Flagging missing other video at {Path}", path);
                    List<int> otherVideoIds = await FlagFileNotFound(libraryPath, path);
                    await _mediator.Publish(
                        new ScannerProgressUpdate(
                            libraryPath.LibraryId,
                            null,
                            null,
                            otherVideoIds.ToArray(),
                            Array.Empty<int>()),
                        cancellationToken);
                }
                else if (Path.GetFileName(path).StartsWith("._", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Removing dot underscore file at {Path}", path);
                    List<int> otherVideoIds = await _otherVideoRepository.DeleteByPath(libraryPath, path);
                    await _mediator.Publish(
                        new ScannerProgressUpdate(
                            libraryPath.LibraryId,
                            null,
                            null,
                            Array.Empty<int>(),
                            otherVideoIds.ToArray()),
                        cancellationToken);
                }
            }

            await _libraryRepository.CleanEtagsForLibraryPath(libraryPath);

            return Unit.Default;
        }
        catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
        {
            return new ScanCanceled();
        }
    }

    private async Task<Either<BaseError, MediaItemScanResult<OtherVideo>>> UpdateMetadata(
        MediaItemScanResult<OtherVideo> result)
    {
        try
        {
            OtherVideo otherVideo = result.Item;
            string path = otherVideo.MediaVersions.Head().MediaFiles.Head().Path;

            Option<string> maybeNfoFile = new List<string> { Path.ChangeExtension(path, "nfo") }
                .Filter(_localFileSystem.FileExists)
                .HeadOrNone();

            if (maybeNfoFile.IsNone)
            {
                if (!Optional(otherVideo.OtherVideoMetadata).Flatten().Any())
                {
                    _logger.LogDebug("Refreshing {Attribute} for {Path}", "Fallback Metadata", path);
                    if (await _localMetadataProvider.RefreshFallbackMetadata(otherVideo))
                    {
                        result.IsUpdated = true;
                    }
                }
            }

            foreach (string nfoFile in maybeNfoFile)
            {
                bool shouldUpdate = Optional(otherVideo.OtherVideoMetadata).Flatten().HeadOrNone().Match(
                    m => m.MetadataKind == MetadataKind.Fallback ||
                         m.DateUpdated != _localFileSystem.GetLastWriteTime(nfoFile),
                    true);

                if (shouldUpdate)
                {
                    _logger.LogDebug("Refreshing {Attribute} from {Path}", "Sidecar Metadata", nfoFile);
                    if (await _localMetadataProvider.RefreshSidecarMetadata(otherVideo, nfoFile))
                    {
                        result.IsUpdated = true;
                    }
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
            return BaseError.New(ex.ToString());
        }
    }

    private async Task<Either<BaseError, MediaItemScanResult<OtherVideo>>> UpdateSubtitles(
        MediaItemScanResult<OtherVideo> result)
    {
        try
        {
            await _localSubtitlesProvider.UpdateSubtitles(result.Item, None, true);
            return result;
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
            return BaseError.New(ex.ToString());
        }
    }
    
    private async Task<Either<BaseError, MediaItemScanResult<OtherVideo>>> UpdateThumbnail(
        MediaItemScanResult<OtherVideo> result,
        CancellationToken cancellationToken)
    {
        try
        {
            OtherVideo otherVideo = result.Item;

            Option<string> maybeThumbnail = LocateThumbnail(otherVideo);
            foreach (string thumbnailFile in maybeThumbnail)
            {
                OtherVideoMetadata metadata = otherVideo.OtherVideoMetadata.Head();
                await RefreshArtwork(thumbnailFile, metadata, ArtworkKind.Thumbnail, None, None, cancellationToken);
            }

            return result;
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
            return BaseError.New(ex.ToString());
        }
    }
    
    private Option<string> LocateThumbnail(OtherVideo otherVideo)
    {
        string path = otherVideo.MediaVersions.Head().MediaFiles.Head().Path;
        return ImageFileExtensions
            .Map(ext => Path.ChangeExtension(path, ext))
            .Filter(f => _localFileSystem.FileExists(f))
            .HeadOrNone();
    }
}
