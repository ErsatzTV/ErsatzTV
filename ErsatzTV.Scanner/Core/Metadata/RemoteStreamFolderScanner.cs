using System.Collections.Immutable;
using Bugsnag;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Images;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.MediaSources;
using ErsatzTV.Core.Metadata;
using ErsatzTV.Core.Streaming;
using ErsatzTV.Scanner.Core.Interfaces.FFmpeg;
using ErsatzTV.Scanner.Core.Interfaces.Metadata;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ErsatzTV.Scanner.Core.Metadata;

public class RemoteStreamFolderScanner : LocalFolderScanner, IRemoteStreamFolderScanner
{
    private readonly IClient _client;
    private readonly ILibraryRepository _libraryRepository;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILocalMetadataProvider _localMetadataProvider;
    private readonly ILogger<RemoteStreamFolderScanner> _logger;
    private readonly IMediaItemRepository _mediaItemRepository;
    private readonly IMediator _mediator;
    private readonly IRemoteStreamRepository _remoteStreamRepository;

    public RemoteStreamFolderScanner(
        ILocalFileSystem localFileSystem,
        ILocalStatisticsProvider localStatisticsProvider,
        ILocalMetadataProvider localMetadataProvider,
        IMetadataRepository metadataRepository,
        IImageCache imageCache,
        IMediator mediator,
        IRemoteStreamRepository remoteStreamRepository,
        ILibraryRepository libraryRepository,
        IMediaItemRepository mediaItemRepository,
        IFFmpegPngService ffmpegPngService,
        ITempFilePool tempFilePool,
        IClient client,
        ILogger<RemoteStreamFolderScanner> logger) : base(
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
        _mediator = mediator;
        _remoteStreamRepository = remoteStreamRepository;
        _libraryRepository = libraryRepository;
        _mediaItemRepository = mediaItemRepository;
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
            IDeserializer deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            decimal progressSpread = progressMax - progressMin;

            var foldersCompleted = 0;

            var allFolders = new System.Collections.Generic.HashSet<string>();
            var folderQueue = new Queue<string>();

            string normalizedLibraryPath = libraryPath.Path.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);
            if (libraryPath.Path != normalizedLibraryPath)
            {
                await _libraryRepository.UpdatePath(libraryPath, normalizedLibraryPath);
            }

            ImmutableHashSet<string> allTrashedItems = await _mediaItemRepository.GetAllTrashedItems(libraryPath);

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
                        [],
                        []),
                    cancellationToken);

                string remoteStreamFolder = folderQueue.Dequeue();
                Option<int> maybeParentFolder =
                    await _libraryRepository.GetParentFolderId(libraryPath, remoteStreamFolder);

                foldersCompleted++;

                var filesForEtag = _localFileSystem.ListFiles(remoteStreamFolder).ToList();

                var allFiles = filesForEtag
                    .Filter(f => RemoteStreamExtensions.Contains(Path.GetExtension(f).Replace(".", string.Empty)))
                    .Filter(f => !Path.GetFileName(f).StartsWith("._", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (string subdirectory in _localFileSystem.ListSubdirectories(remoteStreamFolder)
                             .Filter(ShouldIncludeFolder)
                             .Filter(allFolders.Add)
                             .OrderBy(identity))
                {
                    folderQueue.Enqueue(subdirectory);
                }

                string etag = FolderEtag.Calculate(remoteStreamFolder, _localFileSystem);
                LibraryFolder knownFolder = await _libraryRepository.GetOrAddFolder(
                    libraryPath,
                    maybeParentFolder,
                    remoteStreamFolder);

                if (knownFolder.Etag == etag)
                {
                    if (allFiles.Any(allTrashedItems.Contains))
                    {
                        _logger.LogDebug(
                            "Previously trashed items are now present in folder {Folder}",
                            remoteStreamFolder);
                    }
                    else
                    {
                        // etag matches and no trashed items are now present, continue to next folder
                        continue;
                    }
                }
                else
                {
                    _logger.LogDebug(
                        "UPDATE: Etag has changed for folder {Folder}",
                        remoteStreamFolder);
                }

                var hasErrors = false;

                foreach (string file in allFiles.OrderBy(identity))
                {
                    Either<BaseError, MediaItemScanResult<RemoteStream>> maybeVideo = await _remoteStreamRepository
                        .GetOrAdd(libraryPath, knownFolder, file)
                        .BindT(video => ParseRemoteStreamDefinition(video, deserializer, cancellationToken))
                        .BindT(video => UpdateStatistics(video, ffmpegPath, ffprobePath))
                        .BindT(video => UpdateLibraryFolderId(video, knownFolder))
                        .BindT(UpdateMetadata)
                        //.BindT(video => UpdateThumbnail(video, cancellationToken))
                        //.BindT(UpdateSubtitles)
                        .BindT(FlagNormal);

                    foreach (BaseError error in maybeVideo.LeftToSeq())
                    {
                        _logger.LogWarning("Error processing remote stream at {Path}: {Error}", file, error.Value);
                        hasErrors = true;
                    }

                    foreach (MediaItemScanResult<RemoteStream> result in maybeVideo.RightToSeq())
                    {
                        if (result.IsAdded || result.IsUpdated)
                        {
                            await _mediator.Publish(
                                new ScannerProgressUpdate(
                                    libraryPath.LibraryId,
                                    null,
                                    null,
                                    [result.Item.Id],
                                    []),
                                cancellationToken);
                        }
                    }
                }

                // only do this once per folder and only if all files processed successfully
                if (!hasErrors)
                {
                    await _libraryRepository.SetEtag(libraryPath, knownFolder, remoteStreamFolder, etag);
                }
            }

            foreach (string path in await _remoteStreamRepository.FindRemoteStreamPaths(libraryPath))
            {
                if (!_localFileSystem.FileExists(path))
                {
                    _logger.LogInformation("Flagging missing remote stream at {Path}", path);
                    List<int> remoteStreamIds = await FlagFileNotFound(libraryPath, path);
                    await _mediator.Publish(
                        new ScannerProgressUpdate(
                            libraryPath.LibraryId,
                            null,
                            null,
                            remoteStreamIds.ToArray(),
                            []),
                        cancellationToken);
                }
                else if (Path.GetFileName(path).StartsWith("._", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Removing dot underscore file at {Path}", path);
                    List<int> remoteStreamIds = await _remoteStreamRepository.DeleteByPath(libraryPath, path);
                    await _mediator.Publish(
                        new ScannerProgressUpdate(
                            libraryPath.LibraryId,
                            null,
                            null,
                            [],
                            remoteStreamIds.ToArray()),
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

    private async Task<Either<BaseError, MediaItemScanResult<RemoteStream>>> UpdateLibraryFolderId(
        MediaItemScanResult<RemoteStream> video,
        LibraryFolder libraryFolder)
    {
        MediaFile mediaFile = video.Item.GetHeadVersion().MediaFiles.Head();
        if (mediaFile.LibraryFolderId != libraryFolder.Id)
        {
            await _libraryRepository.UpdateLibraryFolderId(mediaFile, libraryFolder.Id);
            video.IsUpdated = true;
        }

        return video;
    }

    private async Task<Either<BaseError, MediaItemScanResult<RemoteStream>>> ParseRemoteStreamDefinition(
        MediaItemScanResult<RemoteStream> result,
        IDeserializer deserializer,
        CancellationToken cancellationToken)
    {
        try
        {
            RemoteStream remoteStream = result.Item;

            string path = remoteStream.GetHeadVersion().MediaFiles.Head().Path;
            string yaml = await File.ReadAllTextAsync(path, cancellationToken);
            YamlRemoteStreamDefinition definition = deserializer.Deserialize<YamlRemoteStreamDefinition>(yaml);
            if (!definition.IsLive.HasValue)
            {
                return BaseError.New("Remote stream definition is missing required `is_live` property");
            }

            var updated = false;
            if (remoteStream.IsLive != definition.IsLive.Value)
            {
                remoteStream.IsLive = definition.IsLive.Value;
                updated = true;
            }

            if (remoteStream.Url != definition.Url)
            {
                remoteStream.Url = definition.Url;
                updated = true;
            }

            if (remoteStream.Script != definition.Script)
            {
                remoteStream.Script = definition.Script;
                updated = true;
            }

            if (TimeSpan.TryParse(definition.Duration, out TimeSpan duration))
            {
                if (remoteStream.Duration != duration)
                {
                    remoteStream.Duration = duration;
                    updated = true;
                }
            }
            else
            {
                if (remoteStream.Duration is not null)
                {
                    remoteStream.Duration = null;
                    updated = true;
                }
            }

            if (remoteStream.FallbackQuery != definition.FallbackQuery)
            {
                remoteStream.FallbackQuery = definition.FallbackQuery;
                updated = true;
            }

            if (string.IsNullOrEmpty(remoteStream.Url) && string.IsNullOrEmpty(remoteStream.Script))
            {
                return BaseError.New($"`url` or `script` is required in remote stream definition file {path}");
            }

            if (updated)
            {
                await _remoteStreamRepository.UpdateDefinition(remoteStream);
                result.IsUpdated = true;
            }

            return result;
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
            return BaseError.New(ex.ToString());
        }
    }

    private async Task<Either<BaseError, MediaItemScanResult<RemoteStream>>> UpdateMetadata(
        MediaItemScanResult<RemoteStream> result)
    {
        try
        {
            RemoteStream remoteStream = result.Item;
            string path = remoteStream.GetHeadVersion().MediaFiles.Head().Path;
            var shouldUpdate = true;

            foreach (RemoteStreamMetadata remoteStreamMetadata in Optional(remoteStream.RemoteStreamMetadata)
                         .Flatten()
                         .HeadOrNone())
            {
                shouldUpdate = remoteStreamMetadata.MetadataKind == MetadataKind.Fallback ||
                               remoteStreamMetadata.DateUpdated != _localFileSystem.GetLastWriteTime(path);
            }

            if (shouldUpdate)
            {
                remoteStream.RemoteStreamMetadata ??= [];

                _logger.LogDebug("Refreshing {Attribute} for {Path}", "Metadata", path);
                if (await _localMetadataProvider.RefreshTagMetadata(remoteStream))
                {
                    result.IsUpdated = true;
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
}
