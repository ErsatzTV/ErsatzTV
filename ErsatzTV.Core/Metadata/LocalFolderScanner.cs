using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Domain;
using ErsatzTV.Core.Interfaces.Images;
using ErsatzTV.Core.Interfaces.Metadata;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Metadata
{
    public abstract class LocalFolderScanner
    {
        protected static readonly List<string> VideoFileExtensions = new()
        {
            ".mpg", ".mp2", ".mpeg", ".mpe", ".mpv", ".ogg", ".mp4",
            ".m4p", ".m4v", ".avi", ".wmv", ".mov", ".mkv", ".ts"
        };

        protected static readonly List<string> ImageFileExtensions = new()
        {
            "jpg", "jpeg", "png", "gif", "tbn"
        };

        protected static readonly List<string> ExtraFiles = new()
        {
            "behindthescenes", "deleted", "featurette",
            "interview", "scene", "short", "trailer", "other"
        };

        protected static readonly List<string> ExtraDirectories = new List<string>
            {
                "behind the scenes", "deleted scenes", "featurettes",
                "interviews", "scenes", "shorts", "trailers", "other",
                "extras", "specials"
            }
            .Map(s => $"{Path.DirectorySeparatorChar}{s}{Path.DirectorySeparatorChar}")
            .ToList();

        private readonly IImageCache _imageCache;

        private readonly ILocalFileSystem _localFileSystem;
        private readonly ILocalStatisticsProvider _localStatisticsProvider;
        private readonly ILogger _logger;

        protected LocalFolderScanner(
            ILocalFileSystem localFileSystem,
            ILocalStatisticsProvider localStatisticsProvider,
            IImageCache imageCache,
            ILogger logger)
        {
            _localFileSystem = localFileSystem;
            _localStatisticsProvider = localStatisticsProvider;
            _imageCache = imageCache;
            _logger = logger;
        }

        protected async Task<Either<BaseError, T>> UpdateStatistics<T>(T mediaItem, string ffprobePath)
            where T : MediaItem
        {
            try
            {
                if (mediaItem.Statistics is null ||
                    (mediaItem.Statistics.LastWriteTime ?? DateTime.MinValue) <
                    _localFileSystem.GetLastWriteTime(mediaItem.Path))
                {
                    _logger.LogDebug("Refreshing {Attribute} for {Path}", "Statistics", mediaItem.Path);
                    await _localStatisticsProvider.RefreshStatistics(ffprobePath, mediaItem);
                }

                return mediaItem;
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        protected async Task SavePosterToDisk<T>(
            T show,
            string posterPath,
            Func<T, Task<bool>> update,
            int height = 220) where T : IHasAPoster
        {
            byte[] originalBytes = await File.ReadAllBytesAsync(posterPath);
            Either<BaseError, string> maybeHash = await _imageCache.ResizeAndSaveImage(originalBytes, height, null);
            await maybeHash.Match(
                hash =>
                {
                    show.Poster = hash;
                    show.PosterLastWriteTime = _localFileSystem.GetLastWriteTime(posterPath);
                    return update(show);
                },
                error =>
                {
                    _logger.LogWarning("Unable to save poster to disk from {Path}: {Error}", posterPath, error.Value);
                    return Task.CompletedTask;
                });
        }

        protected Task<Either<BaseError, string>> SavePosterToDisk(string posterPath, int height = 220) =>
            File.ReadAllBytesAsync(posterPath)
                .Bind(bytes => _imageCache.ResizeAndSaveImage(bytes, height, null));
    }
}
