using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Images;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using LanguageExt;
using MediatR;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Core.Metadata
{
    public class SongFolderScanner : LocalFolderScanner, ISongFolderScanner
    {
        private readonly ILocalFileSystem _localFileSystem;
        private readonly ILocalMetadataProvider _localMetadataProvider;
        private readonly IMediator _mediator;
        private readonly ISearchIndex _searchIndex;
        private readonly ISearchRepository _searchRepository;
        private readonly ISongRepository _songRepository;
        private readonly ILibraryRepository _libraryRepository;
        private readonly ILogger<SongFolderScanner> _logger;

        public SongFolderScanner(
            ILocalFileSystem localFileSystem,
            ILocalStatisticsProvider localStatisticsProvider,
            ILocalMetadataProvider localMetadataProvider,
            IMetadataRepository metadataRepository,
            IImageCache imageCache,
            IMediator mediator,
            ISearchIndex searchIndex,
            ISearchRepository searchRepository,
            ISongRepository songRepository,
            ILibraryRepository libraryRepository,
            IFFmpegProcessService ffmpegProcessService,
            ITempFilePool tempFilePool,
            ILogger<SongFolderScanner> logger) : base(
            localFileSystem,
            localStatisticsProvider,
            metadataRepository,
            imageCache,
            ffmpegProcessService,
            tempFilePool,
            logger)
        {
            _localFileSystem = localFileSystem;
            _localMetadataProvider = localMetadataProvider;
            _mediator = mediator;
            _searchIndex = searchIndex;
            _searchRepository = searchRepository;
            _songRepository = songRepository;
            _libraryRepository = libraryRepository;
            _logger = logger;
        }

        public async Task<Either<BaseError, Unit>> ScanFolder(
            LibraryPath libraryPath,
            string ffprobePath,
            string ffmpegPath,
            decimal progressMin,
            decimal progressMax)
        {
            decimal progressSpread = progressMax - progressMin;

            if (!_localFileSystem.IsLibraryPathAccessible(libraryPath))
            {
                return new MediaSourceInaccessible();
            }

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
                decimal percentCompletion = (decimal)foldersCompleted / (foldersCompleted + folderQueue.Count);
                await _mediator.Publish(
                    new LibraryScanProgress(libraryPath.LibraryId, progressMin + percentCompletion * progressSpread));

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
                if (!allFiles.Any() || await knownFolder.Map(f => f.Etag ?? string.Empty).IfNoneAsync(string.Empty) == etag)
                {
                    continue;
                }

                _logger.LogDebug(
                    "UPDATE: Etag has changed for folder {Folder}",
                    songFolder);

                foreach (string file in allFiles.OrderBy(identity))
                {
                    Either<BaseError, MediaItemScanResult<Song>> maybeSong = await _songRepository
                        .GetOrAdd(libraryPath, file)
                        .BindT(video => UpdateStatistics(video, ffprobePath))
                        .BindT(video => UpdateMetadata(video, ffprobePath))
                        .BindT(video => UpdateThumbnail(video, ffmpegPath));

                    await maybeSong.Match(
                        async result =>
                        {
                            if (result.IsAdded)
                            {
                                await _searchIndex.AddItems(_searchRepository, new List<MediaItem> { result.Item });
                            }
                            else if (result.IsUpdated)
                            {
                                await _searchIndex.UpdateItems(_searchRepository, new List<MediaItem> { result.Item });
                            }

                            await _libraryRepository.SetEtag(libraryPath, knownFolder, songFolder, etag);
                        },
                        error =>
                        {
                            _logger.LogWarning("Error processing song at {Path}: {Error}", file, error.Value);
                            return Task.CompletedTask;
                        });
                }
            }

            foreach (string path in await _songRepository.FindSongPaths(libraryPath))
            {
                if (!_localFileSystem.FileExists(path))
                {
                    _logger.LogInformation("Removing missing song at {Path}", path);
                    List<int> songIds = await _songRepository.DeleteByPath(libraryPath, path);
                    await _searchIndex.RemoveItems(songIds);
                }
                else if (Path.GetFileName(path).StartsWith("._"))
                {
                    _logger.LogInformation("Removing dot underscore file at {Path}", path);
                    List<int> songIds = await _songRepository.DeleteByPath(libraryPath, path);
                    await _searchIndex.RemoveItems(songIds);
                }
            }
            
            await _libraryRepository.CleanEtagsForLibraryPath(libraryPath);

            _searchIndex.Commit();
            return Unit.Default;
        }

        private async Task<Either<BaseError, MediaItemScanResult<Song>>> UpdateMetadata(
            MediaItemScanResult<Song> result, string ffprobePath)
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
                return BaseError.New(ex.ToString());
            }
        }

        private async Task<Either<BaseError, MediaItemScanResult<Song>>> UpdateThumbnail(
            MediaItemScanResult<Song> result,
            string ffmpegPath)
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

                await LocateThumbnail(song).Match(
                    async thumbnailFile =>
                    {
                        SongMetadata metadata = song.SongMetadata.Head();
                        await RefreshArtwork(thumbnailFile, metadata, ArtworkKind.Thumbnail, ffmpegPath, None);
                    },
                    () => ExtractEmbeddedArtwork(song, ffmpegPath));
        
                return result;
            }
            catch (Exception ex)
            {
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

        private async Task ExtractEmbeddedArtwork(Song song, string ffmpegPath)
        {
            Option<MediaStream> maybeArtworkStream = Optional(song.GetHeadVersion().Streams.Find(ms => ms.AttachedPic));
            foreach (MediaStream artworkStream in maybeArtworkStream)
            {
                await RefreshArtwork(
                    song.GetHeadVersion().MediaFiles.Head().Path,
                    song.SongMetadata.Head(),
                    ArtworkKind.Thumbnail,
                    ffmpegPath,
                    artworkStream.Index);
            }
        }
    }
}
