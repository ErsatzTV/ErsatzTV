using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Images;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Metadata
{
    public abstract class LocalFolderScanner
    {
        public static readonly List<string> VideoFileExtensions = new()
        {
            ".mpg", ".mp2", ".mpeg", ".mpe", ".mpv", ".ogg", ".mp4",
            ".m4p", ".m4v", ".avi", ".wmv", ".mov", ".mkv", ".ts", ".webm"
        };

        public static readonly List<string> AudioFileExtensions = new()
        {
            ".aac", ".alac", ".flac", ".mp3", ".m4a", ".wav", ".wma"
        };

        public static readonly List<string> ImageFileExtensions = new()
        {
            "jpg", "jpeg", "png", "gif", "tbn"
        };

        public static readonly List<string> ExtraFiles = new()
        {
            "behindthescenes", "deleted", "featurette",
            "interview", "scene", "short", "trailer", "other"
        };

        public static readonly List<string> ExtraDirectories = new List<string>
            {
                "behind the scenes", "deleted scenes", "featurettes",
                "interviews", "scenes", "shorts", "trailers", "other",
                "extras", "specials"
            }
            .Map(s => $"{Path.DirectorySeparatorChar}{s}{Path.DirectorySeparatorChar}")
            .ToList();

        private readonly IImageCache _imageCache;
        private readonly IFFmpegProcessService _ffmpegProcessService;
        private readonly ITempFilePool _tempFilePool;

        private readonly ILocalFileSystem _localFileSystem;
        private readonly ILocalStatisticsProvider _localStatisticsProvider;
        private readonly ILogger _logger;
        private readonly IMetadataRepository _metadataRepository;

        protected LocalFolderScanner(
            ILocalFileSystem localFileSystem,
            ILocalStatisticsProvider localStatisticsProvider,
            IMetadataRepository metadataRepository,
            IImageCache imageCache,
            IFFmpegProcessService ffmpegProcessService, 
            ITempFilePool tempFilePool,
            ILogger logger)
        {
            _localFileSystem = localFileSystem;
            _localStatisticsProvider = localStatisticsProvider;
            _metadataRepository = metadataRepository;
            _imageCache = imageCache;
            _ffmpegProcessService = ffmpegProcessService;
            _tempFilePool = tempFilePool;
            _logger = logger;
        }

        protected async Task<Either<BaseError, MediaItemScanResult<T>>> UpdateStatistics<T>(
            MediaItemScanResult<T> mediaItem,
            string ffprobePath)
            where T : MediaItem
        {
            try
            {
                MediaVersion version = mediaItem.Item.GetHeadVersion();

                string path = version.MediaFiles.Head().Path;

                if (version.DateUpdated != _localFileSystem.GetLastWriteTime(path) || !version.Streams.Any())
                {
                    _logger.LogDebug("Refreshing {Attribute} for {Path}", "Statistics", path);
                    Either<BaseError, bool> refreshResult =
                        await _localStatisticsProvider.RefreshStatistics(ffprobePath, mediaItem.Item);
                    refreshResult.Match(
                        result =>
                        {
                            if (result)
                            {
                                mediaItem.IsUpdated = true;
                            }
                        },
                        error =>
                            _logger.LogWarning(
                                "Unable to refresh {Attribute} for media item {Path}. Error: {Error}",
                                "Statistics",
                                path,
                                error.Value));
                }

                return mediaItem;
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        protected async Task<bool> RefreshArtwork(string artworkFile, Domain.Metadata metadata, ArtworkKind artworkKind, Option<string> ffmpegPath)
        {
            DateTime lastWriteTime = _localFileSystem.GetLastWriteTime(artworkFile);

            metadata.Artwork ??= new List<Artwork>();

            Option<Artwork> maybeArtwork = metadata.Artwork.FirstOrDefault(a => a.ArtworkKind == artworkKind);

            bool shouldRefresh = maybeArtwork.Match(
                artwork => lastWriteTime.Subtract(artwork.DateUpdated) > TimeSpan.FromSeconds(1),
                true);

            if (shouldRefresh)
            {
                try
                {
                    _logger.LogDebug("Refreshing {Attribute} from {Path}", artworkKind, artworkFile);

                    // if ffmpeg path is passed, we want to convert to png
                    foreach (string path in ffmpegPath)
                    {
                        string tempName = _tempFilePool.GetNextTempFile(TempFileCategory.CoverArt);
                        using Process process = _ffmpegProcessService.ConvertToPng(path, artworkFile, tempName);
                        process.Start();
                        await process.WaitForExitAsync();

                        artworkFile = tempName;
                    }

                    Either<BaseError, string> maybeCacheName =
                        await _imageCache.CopyArtworkToCache(artworkFile, artworkKind);

                    return await maybeCacheName.Match(
                        async cacheName =>
                        {
                            await maybeArtwork.Match(
                                async artwork =>
                                {
                                    artwork.Path = cacheName;
                                    artwork.DateUpdated = lastWriteTime;
                                    await _metadataRepository.UpdateArtworkPath(artwork);
                                },
                                async () =>
                                {
                                    var artwork = new Artwork
                                    {
                                        Path = cacheName,
                                        DateAdded = DateTime.UtcNow,
                                        DateUpdated = lastWriteTime,
                                        ArtworkKind = artworkKind
                                    };
                                    metadata.Artwork.Add(artwork);
                                    await _metadataRepository.AddArtwork(metadata, artwork);
                                });

                            return true;
                        },
                        error =>
                        {
                            _logger.LogDebug("Failed to cache artwork from {Path}: {Error}", artworkFile, error.Value);
                            return Task.FromResult(false);
                        });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error refreshing artwork");
                }
            }

            return false;
        }

        protected bool ShouldIncludeFolder(string folder) =>
            !Path.GetFileName(folder).StartsWith('.') &&
            !_localFileSystem.FileExists(Path.Combine(folder, ".etvignore"));
    }
}
