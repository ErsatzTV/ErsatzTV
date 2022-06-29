using Bugsnag;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Images;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Metadata;

public class SongFolderScanner : LocalFolderScanner, ISongFolderScanner
{
    private readonly IClient _client;
    private readonly ILibraryRepository _libraryRepository;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILocalMetadataProvider _localMetadataProvider;
    private readonly ILogger<SongFolderScanner> _logger;
    private readonly IMediator _mediator;
    private readonly ISearchIndex _searchIndex;
    private readonly ISearchRepository _searchRepository;
    private readonly IFallbackMetadataProvider _fallbackMetadataProvider;
    private readonly ISongRepository _songRepository;

    public SongFolderScanner(
        ILocalFileSystem localFileSystem,
        ILocalStatisticsProvider localStatisticsProvider,
        ILocalMetadataProvider localMetadataProvider,
        IMetadataRepository metadataRepository,
        IImageCache imageCache,
        IMediator mediator,
        ISearchIndex searchIndex,
        ISearchRepository searchRepository,
        IFallbackMetadataProvider fallbackMetadataProvider,
        ISongRepository songRepository,
        ILibraryRepository libraryRepository,
        IMediaItemRepository mediaItemRepository,
        IFFmpegProcessService ffmpegProcessService,
        ITempFilePool tempFilePool,
        IClient client,
        ILogger<SongFolderScanner> logger) : base(
        localFileSystem,
        localStatisticsProvider,
        metadataRepository,
        mediaItemRepository,
        imageCache,
        ffmpegProcessService,
        tempFilePool,
        client,
        logger)
    {
        _localFileSystem = localFileSystem;
        _localMetadataProvider = localMetadataProvider;
        _mediator = mediator;
        _searchIndex = searchIndex;
        _searchRepository = searchRepository;
        _fallbackMetadataProvider = fallbackMetadataProvider;
        _songRepository = songRepository;
        _libraryRepository = libraryRepository;
        _client = client;
        _logger = logger;
    }

    public async Task<Either<BaseError, Unit>> ScanFolder(
        LibraryPath libraryPath,
        string ffprobePath,
        string ffmpegPath,
        decimal progressMin,
        decimal progressMax,
        CancellationToken cancellationToken)
    {
        try
        {
            decimal progressSpread = progressMax - progressMin;

            var foldersCompleted = 0;

            var folderQueue = new Queue<string>();

            if (ShouldIncludeFolder(libraryPath.Path))
            {
                folderQueue.Enqueue(libraryPath.Path);
            }

            foreach (string folder in _localFileSystem.ListSubdirectories(libraryPath.Path)
                         .Filter(ShouldIncludeFolder)
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
                    new LibraryScanProgress(libraryPath.LibraryId, progressMin + percentCompletion * progressSpread),
                    cancellationToken);

                string songFolder = folderQueue.Dequeue();
                foldersCompleted++;

                var filesForEtag = _localFileSystem.ListFiles(songFolder).ToList();

                var allFiles = filesForEtag
                    .Filter(f => AudioFileExtensions.Contains(Path.GetExtension(f)))
                    .Filter(f => !Path.GetFileName(f).StartsWith("._"))
                    .ToList();

                foreach (string subdirectory in _localFileSystem.ListSubdirectories(songFolder)
                             .Filter(ShouldIncludeFolder)
                             .OrderBy(identity))
                {
                    folderQueue.Enqueue(subdirectory);
                }

                string etag = FolderEtag.Calculate(songFolder, _localFileSystem);
                Option<LibraryFolder> knownFolder = libraryPath.LibraryFolders
                    .Filter(f => f.Path == songFolder)
                    .HeadOrNone();

                // skip folder if etag matches
                if (!allFiles.Any() || await knownFolder.Map(f => f.Etag ?? string.Empty).IfNoneAsync(string.Empty) ==
                    etag)
                {
                    continue;
                }

                _logger.LogDebug(
                    "UPDATE: Etag has changed for folder {Folder}",
                    songFolder);

                var hasErrors = false;

                foreach (string file in allFiles.OrderBy(identity))
                {
                    Either<BaseError, MediaItemScanResult<Song>> maybeSong = await _songRepository
                        .GetOrAdd(libraryPath, file)
                        .BindT(video => UpdateStatistics(video, ffmpegPath, ffprobePath))
                        .BindT(video => UpdateMetadata(video, ffprobePath))
                        .BindT(video => UpdateThumbnail(video, ffmpegPath, cancellationToken))
                        .BindT(FlagNormal);

                    foreach (BaseError error in maybeSong.LeftToSeq())
                    {
                        _logger.LogWarning("Error processing song at {Path}: {Error}", file, error.Value);
                        hasErrors = true;
                    }

                    foreach (MediaItemScanResult<Song> result in maybeSong.RightToSeq())
                    {
                        if (result.IsAdded || result.IsUpdated)
                        {
                            await _searchIndex.RebuildItems(_searchRepository, _fallbackMetadataProvider, new List<int> { result.Item.Id });
                        }
                    }
                }

                // only do this once per folder and only if all files processed successfully
                if (!hasErrors)
                {
                    await _libraryRepository.SetEtag(libraryPath, knownFolder, songFolder, etag);
                }
            }

            foreach (string path in await _songRepository.FindSongPaths(libraryPath))
            {
                if (!_localFileSystem.FileExists(path))
                {
                    _logger.LogInformation("Flagging missing song at {Path}", path);
                    List<int> songIds = await FlagFileNotFound(libraryPath, path);
                    await _searchIndex.RebuildItems(_searchRepository, _fallbackMetadataProvider, songIds);
                }
                else if (Path.GetFileName(path).StartsWith("._"))
                {
                    _logger.LogInformation("Removing dot underscore file at {Path}", path);
                    List<int> songIds = await _songRepository.DeleteByPath(libraryPath, path);
                    await _searchIndex.RemoveItems(songIds);
                }
            }

            await _libraryRepository.CleanEtagsForLibraryPath(libraryPath);

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

    private async Task<Either<BaseError, MediaItemScanResult<Song>>> UpdateMetadata(
        MediaItemScanResult<Song> result,
        string ffprobePath)
    {
        try
        {
            Song song = result.Item;
            string path = song.GetHeadVersion().MediaFiles.Head().Path;

            bool shouldUpdate = Optional(song.SongMetadata).Flatten().HeadOrNone().Match(
                m => m.MetadataKind == MetadataKind.Fallback ||
                     m.DateUpdated != _localFileSystem.GetLastWriteTime(path),
                true);

            if (shouldUpdate)
            {
                song.SongMetadata ??= new List<SongMetadata>();

                _logger.LogDebug("Refreshing {Attribute} for {Path}", "Metadata", path);
                if (await _localMetadataProvider.RefreshTagMetadata(song, ffprobePath))
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

    private async Task<Either<BaseError, MediaItemScanResult<Song>>> UpdateThumbnail(
        MediaItemScanResult<Song> result,
        string ffmpegPath,
        CancellationToken cancellationToken)
    {
        try
        {
            // reload the song from the database at this point
            if (result.IsAdded)
            {
                LibraryPath libraryPath = result.Item.LibraryPath;
                string path = result.Item.GetHeadVersion().MediaFiles.Head().Path;
                foreach (MediaItemScanResult<Song> s in (await _songRepository.GetOrAdd(libraryPath, path))
                         .RightToSeq())
                {
                    result.Item = s.Item;
                }
            }

            Song song = result.Item;
            Option<string> maybeThumbnail = LocateThumbnail(song);
            if (maybeThumbnail.IsNone)
            {
                await ExtractEmbeddedArtwork(song, ffmpegPath, cancellationToken);
            }


            foreach (string thumbnailFile in maybeThumbnail)
            {
                SongMetadata metadata = song.SongMetadata.Head();
                await RefreshArtwork(
                    thumbnailFile,
                    metadata,
                    ArtworkKind.Thumbnail,
                    ffmpegPath,
                    None,
                    cancellationToken);
            }

            return result;
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
            return BaseError.New(ex.ToString());
        }
    }

    private Option<string> LocateThumbnail(Song song)
    {
        string path = song.MediaVersions.Head().MediaFiles.Head().Path;
        Option<DirectoryInfo> parent = Optional(Directory.GetParent(path));

        return parent.Map(
            di =>
            {
                string coverPath = Path.Combine(di.FullName, "cover.jpg");
                return ImageFileExtensions
                    .Map(ext => Path.ChangeExtension(coverPath, ext))
                    .Filter(f => _localFileSystem.FileExists(f))
                    .HeadOrNone();
            }).Flatten();
    }

    private async Task ExtractEmbeddedArtwork(Song song, string ffmpegPath, CancellationToken cancellationToken)
    {
        Option<MediaStream> maybeArtworkStream = Optional(song.GetHeadVersion().Streams.Find(ms => ms.AttachedPic));
        foreach (MediaStream artworkStream in maybeArtworkStream)
        {
            await RefreshArtwork(
                song.GetHeadVersion().MediaFiles.Head().Path,
                song.SongMetadata.Head(),
                ArtworkKind.Thumbnail,
                ffmpegPath,
                artworkStream.Index,
                cancellationToken);
        }
    }
}
