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
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Metadata
{
    public class MusicVideoFolderScanner : LocalFolderScanner, IMusicVideoFolderScanner
    {
        private readonly ILocalFileSystem _localFileSystem;
        private readonly ILocalMetadataProvider _localMetadataProvider;
        private readonly ILogger<MusicVideoFolderScanner> _logger;
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
            _logger = logger;
        }

        public async Task<Either<BaseError, Unit>> ScanFolder(
            LibraryPath libraryPath,
            string ffprobePath,
            DateTimeOffset lastScan)
        {
            if (!_localFileSystem.IsLibraryPathAccessible(libraryPath))
            {
                return new MediaSourceInaccessible();
            }

            var folderQueue = new Queue<string>();
            foreach (string folder in _localFileSystem.ListSubdirectories(libraryPath.Path).OrderBy(identity))
            {
                folderQueue.Enqueue(folder);
            }

            while (folderQueue.Count > 0)
            {
                string movieFolder = folderQueue.Dequeue();

                var allFiles = _localFileSystem.ListFiles(movieFolder)
                    .Filter(f => VideoFileExtensions.Contains(Path.GetExtension(f)))
                    .Filter(
                        f => !ExtraFiles.Any(
                            e => Path.GetFileNameWithoutExtension(f).EndsWith(e, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                if (allFiles.Count == 0)
                {
                    foreach (string subdirectory in _localFileSystem.ListSubdirectories(movieFolder).OrderBy(identity))
                    {
                        folderQueue.Enqueue(subdirectory);
                    }

                    continue;
                }

                if (_localFileSystem.GetLastWriteTime(movieFolder) < lastScan)
                {
                    continue;
                }

                foreach (string file in allFiles.OrderBy(identity))
                {
                    // TODO: figure out how to rebuild playouts
                    Either<BaseError, MediaItemScanResult<MusicVideo>> maybeMusicVideo =
                        await FindOrCreateMusicVideo(libraryPath, file)
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

            return Unit.Default;
        }

        private async Task<Either<BaseError, MediaItemScanResult<MusicVideo>>> FindOrCreateMusicVideo(
            LibraryPath libraryPath,
            string filePath)
        {
            Option<MusicVideoMetadata> maybeMetadata = await _localMetadataProvider.GetMetadataForMusicVideo(filePath);
            return await maybeMetadata.Match(
                async metadata =>
                {
                    Option<MusicVideo> maybeMusicVideo = await _musicVideoRepository.GetByMetadata(libraryPath, metadata);
                    return await maybeMusicVideo.Match(
                        musicVideo =>
                            Right<BaseError, MediaItemScanResult<MusicVideo>>(
                                    new MediaItemScanResult<MusicVideo>(musicVideo))
                                .AsTask(),
                        async () => await _musicVideoRepository.Add(libraryPath, filePath, metadata));
                },
                () => Left<BaseError, MediaItemScanResult<MusicVideo>>(
                    BaseError.New("Unable to locate metadata for music video")).AsTask());
        }

        private async Task<Either<BaseError, MediaItemScanResult<MusicVideo>>> UpdateMetadata(
            MediaItemScanResult<MusicVideo> result)
        {
            try
            {
                MusicVideo musicVideo = result.Item;
                return await LocateNfoFile(musicVideo).Match<Task<Either<BaseError, MediaItemScanResult<MusicVideo>>>>(
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

                        return result;
                    },
                    () => Left<BaseError, MediaItemScanResult<MusicVideo>>(
                        BaseError.New("Unable to locate metadata for music video")).AsTask());
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
                    async posterFile =>
                    {
                        MusicVideoMetadata metadata = musicVideo.MusicVideoMetadata.Head();
                        await RefreshArtwork(posterFile, metadata, ArtworkKind.Thumbnail);
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
