using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Images;
using ErsatzTV.Core.Interfaces.Metadata;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Metadata
{
    public abstract class LocalFolderScanner
    {
        private static readonly SHA1CryptoServiceProvider Crypto;

        public static readonly List<string> VideoFileExtensions = new()
        {
            ".mpg", ".mp2", ".mpeg", ".mpe", ".mpv", ".ogg", ".mp4",
            ".m4p", ".m4v", ".avi", ".wmv", ".mov", ".mkv", ".ts"
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

        private readonly ILocalFileSystem _localFileSystem;
        private readonly ILocalStatisticsProvider _localStatisticsProvider;
        private readonly ILogger _logger;

        static LocalFolderScanner() => Crypto = new SHA1CryptoServiceProvider();

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

        protected bool RefreshArtwork(string artworkFile, Domain.Metadata metadata, ArtworkKind artworkKind)
        {
            DateTime lastWriteTime = _localFileSystem.GetLastWriteTime(artworkFile);

            Option<Artwork> maybePoster =
                metadata.Artwork.FirstOrDefault(a => a.ArtworkKind == artworkKind);

            bool shouldRefresh = maybePoster.Match(
                artwork => artwork.DateUpdated < lastWriteTime,
                true);

            if (shouldRefresh)
            {
                _logger.LogDebug("Refreshing {Attribute} from {Path}", artworkKind, artworkFile);
                string cacheName = CopyArtworkToCache(artworkFile, artworkKind);

                maybePoster.Match(
                    artwork =>
                    {
                        artwork.Path = cacheName;
                        artwork.DateUpdated = lastWriteTime;
                    },
                    () =>
                    {
                        var artwork = new Artwork
                        {
                            Path = cacheName,
                            DateAdded = DateTime.UtcNow,
                            DateUpdated = lastWriteTime,
                            ArtworkKind = artworkKind
                        };

                        metadata.Artwork.Add(artwork);
                    });

                return true;
            }

            return false;
        }

        private string CopyArtworkToCache(string path, ArtworkKind artworkKind)
        {
            var filenameKey = $"{path}:{_localFileSystem.GetLastWriteTime(path).ToFileTimeUtc()}";
            byte[] hash = Crypto.ComputeHash(Encoding.UTF8.GetBytes(filenameKey));
            string hex = BitConverter.ToString(hash).Replace("-", string.Empty);
            string subfolder = hex.Substring(0, 2);
            string baseFolder = artworkKind switch
            {
                ArtworkKind.Poster => Path.Combine(FileSystemLayout.PosterCacheFolder, subfolder),
                ArtworkKind.Thumbnail => Path.Combine(FileSystemLayout.ThumbnailCacheFolder, subfolder),
                _ => FileSystemLayout.ImageCacheFolder
            };
            string target = Path.Combine(baseFolder, hex);
            _localFileSystem.CopyFile(path, target);

            return hex;
        }
    }
}
