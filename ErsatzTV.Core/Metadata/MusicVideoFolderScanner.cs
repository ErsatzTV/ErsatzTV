using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Errors;
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
    public class MusicVideoFolderScanner : LocalFolderScanner, IMusicVideoFolderScanner
    {
        private readonly ILocalFileSystem _localFileSystem;
        private readonly ILocalMetadataProvider _localMetadataProvider;
        private readonly ILogger<MusicVideoFolderScanner> _logger;
        private readonly IMediator _mediator;
        private readonly IMusicVideoRepository _musicVideoRepository;
        private readonly ISearchIndex _searchIndex;

        public MusicVideoFolderScanner(
            ILocalFileSystem localFileSystem,
            ILocalStatisticsProvider localStatisticsProvider,
            ILocalMetadataProvider localMetadataProvider,
            IMetadataRepository metadataRepository,
            IImageCache imageCache,
            ISearchIndex searchIndex,
            IMusicVideoRepository musicVideoRepository,
            IMediator mediator,
            ILogger<MusicVideoFolderScanner> logger) : base(
            localFileSystem,
            localStatisticsProvider,
            metadataRepository,
            imageCache,
            logger)
        {
            _localFileSystem = localFileSystem;
            _localMetadataProvider = localMetadataProvider;
            _searchIndex = searchIndex;
            _musicVideoRepository = musicVideoRepository;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<Either<BaseError, Unit>> ScanFolder(
            LibraryPath libraryPath,
            string ffprobePath,
            DateTimeOffset lastScan,
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
            folderQueue.Enqueue(libraryPath.Path);

            while (folderQueue.Count > 0)
            {
                decimal percentCompletion = (decimal) foldersCompleted / (foldersCompleted + folderQueue.Count);
                await _mediator.Publish(
                    new LibraryScanProgress(libraryPath.LibraryId, progressMin + percentCompletion * progressSpread));

                string musicVideoFolder = folderQueue.Dequeue();
                foldersCompleted++;

                var allFiles = _localFileSystem.ListFiles(musicVideoFolder)
                    .Filter(f => VideoFileExtensions.Contains(Path.GetExtension(f)))
                    .Filter(f => f.Contains(" - "))
                    .ToList();

                foreach (string subdirectory in _localFileSystem.ListSubdirectories(musicVideoFolder)
                    .OrderBy(identity))
                {
                    folderQueue.Enqueue(subdirectory);
                }

                if (_localFileSystem.GetLastWriteTime(musicVideoFolder) < lastScan)
                {
                    continue;
                }

                foreach (string file in allFiles.OrderBy(identity))
                {
                    // TODO: figure out how to rebuild playouts
                    Either<BaseError, MediaItemScanResult<MusicVideo>> maybeMusicVideo = await _musicVideoRepository
                        .GetOrAdd(libraryPath, file)
                        .BindT(musicVideo => UpdateStatistics(musicVideo, ffprobePath))
                        .BindT(UpdateMetadata)
                        .BindT(UpdateThumbnail);

                    await maybeMusicVideo.Match(
                        async result =>
                        {
                            if (result.IsAdded)
                            {
                                await _searchIndex.AddItems(new List<MediaItem> { result.Item });
                            }
                            else if (result.IsUpdated)
                            {
                                await _searchIndex.UpdateItems(new List<MediaItem> { result.Item });
                            }
                        },
                        error =>
                        {
                            _logger.LogWarning("Error processing music video at {Path}: {Error}", file, error.Value);
                            return Task.CompletedTask;
                        });
                }
            }

            foreach (string path in await _musicVideoRepository.FindMusicVideoPaths(libraryPath))
            {
                if (!_localFileSystem.FileExists(path))
                {
                    _logger.LogInformation("Removing missing music video at {Path}", path);
                    List<int> ids = await _musicVideoRepository.DeleteByPath(libraryPath, path);
                    await _searchIndex.RemoveItems(ids);
                }
            }
            
            _searchIndex.Commit();
            return Unit.Default;
        }

        private async Task<Either<BaseError, MediaItemScanResult<MusicVideo>>> UpdateMetadata(
            MediaItemScanResult<MusicVideo> result)
        {
            try
            {
                MusicVideo musicVideo = result.Item;
                await LocateNfoFile(musicVideo).Match(
                    async nfoFile =>
                    {
                        bool shouldUpdate = Optional(musicVideo.MusicVideoMetadata).Flatten().HeadOrNone().Match(
                            m => m.MetadataKind == MetadataKind.Fallback ||
                                 m.DateUpdated < _localFileSystem.GetLastWriteTime(nfoFile),
                            true);

                        if (shouldUpdate)
                        {
                            _logger.LogDebug("Refreshing {Attribute} from {Path}", "Sidecar Metadata", nfoFile);
                            if (await _localMetadataProvider.RefreshSidecarMetadata(musicVideo, nfoFile))
                            {
                                result.IsUpdated = true;
                            }
                        }
                    },
                    async () =>
                    {
                        if (!Optional(musicVideo.MusicVideoMetadata).Flatten().Any())
                        {
                            string path = musicVideo.MediaVersions.Head().MediaFiles.Head().Path;
                            _logger.LogDebug("Refreshing {Attribute} for {Path}", "Fallback Metadata", path);
                            if (await _localMetadataProvider.RefreshFallbackMetadata(musicVideo))
                            {
                                result.IsUpdated = true;
                            }
                        }
                    });

                return result;
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.ToString());
            }
        }

        private Option<string> LocateNfoFile(MusicVideo musicVideo)
        {
            string path = musicVideo.MediaVersions.Head().MediaFiles.Head().Path;
            return Optional(Path.ChangeExtension(path, "nfo"))
                .Filter(s => _localFileSystem.FileExists(s))
                .HeadOrNone();
        }

        private async Task<Either<BaseError, MediaItemScanResult<MusicVideo>>> UpdateThumbnail(
            MediaItemScanResult<MusicVideo> result)
        {
            try
            {
                MusicVideo musicVideo = result.Item;
                await LocateThumbnail(musicVideo).IfSomeAsync(
                    async thumbnailFile =>
                    {
                        MusicVideoMetadata metadata = musicVideo.MusicVideoMetadata.Head();
                        await RefreshArtwork(thumbnailFile, metadata, ArtworkKind.Thumbnail);
                    });

                return result;
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.ToString());
            }
        }

        private Option<string> LocateThumbnail(MusicVideo musicVideo)
        {
            string path = musicVideo.MediaVersions.Head().MediaFiles.Head().Path;
            return ImageFileExtensions
                .Map(ext => Path.ChangeExtension(path, ext))
                .Filter(f => _localFileSystem.FileExists(f))
                .HeadOrNone();
        }
    }
}
