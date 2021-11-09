using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Images;
using ErsatzTV.Core.Interfaces.Metadata;
using LanguageExt;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace ErsatzTV.Infrastructure.Images
{
    public class ImageCache : IImageCache
    {
        private static readonly SHA1 Crypto;
        private readonly ILocalFileSystem _localFileSystem;
        private readonly ILogger<ImageCache> _logger;
        private readonly IMemoryCache _memoryCache;

        static ImageCache() => Crypto = SHA1.Create();

        public ImageCache(ILocalFileSystem localFileSystem, IMemoryCache memoryCache, ILogger<ImageCache> logger)
        {
            _localFileSystem = localFileSystem;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public async Task<Either<BaseError, byte[]>> ResizeImage(byte[] imageBuffer, int height)
        {
            await using var inStream = new MemoryStream(imageBuffer);
            using Image image = await Image.LoadAsync(inStream);

            var size = new Size { Height = height };

            image.Mutate(
                i => i.Resize(
                    new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = size
                    }));

            await using var outStream = new MemoryStream();
            await image.SaveAsync(outStream, new JpegEncoder { Quality = 90 });

            return outStream.ToArray();
        }

        public async Task<Either<BaseError, string>> SaveArtworkToCache(byte[] imageBuffer, ArtworkKind artworkKind)
        {
            try
            {
                byte[] hash = Crypto.ComputeHash(imageBuffer);
                string hex = BitConverter.ToString(hash).Replace("-", string.Empty);
                string subfolder = hex[..2];
                string baseFolder = artworkKind switch
                {
                    ArtworkKind.Poster => Path.Combine(FileSystemLayout.PosterCacheFolder, subfolder),
                    ArtworkKind.Thumbnail => Path.Combine(FileSystemLayout.ThumbnailCacheFolder, subfolder),
                    ArtworkKind.Logo => Path.Combine(FileSystemLayout.LogoCacheFolder, subfolder),
                    ArtworkKind.FanArt => Path.Combine(FileSystemLayout.FanArtCacheFolder, subfolder),
                    ArtworkKind.Watermark => Path.Combine(FileSystemLayout.WatermarkCacheFolder, subfolder),
                    _ => FileSystemLayout.LegacyImageCacheFolder
                };
                string target = Path.Combine(baseFolder, hex);

                if (!Directory.Exists(baseFolder))
                {
                    Directory.CreateDirectory(baseFolder);
                }

                await File.WriteAllBytesAsync(target, imageBuffer);
                return hex;
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        public async Task<Either<BaseError, string>> CopyArtworkToCache(string path, ArtworkKind artworkKind)
        {
            try
            {
                var filenameKey = $"{path}:{_localFileSystem.GetLastWriteTime(path).ToFileTimeUtc()}";
                byte[] hash = Crypto.ComputeHash(Encoding.UTF8.GetBytes(filenameKey));
                string hex = BitConverter.ToString(hash).Replace("-", string.Empty);
                string subfolder = hex[..2];
                string baseFolder = artworkKind switch
                {
                    ArtworkKind.Poster => Path.Combine(FileSystemLayout.PosterCacheFolder, subfolder),
                    ArtworkKind.Thumbnail => Path.Combine(FileSystemLayout.ThumbnailCacheFolder, subfolder),
                    ArtworkKind.Logo => Path.Combine(FileSystemLayout.LogoCacheFolder, subfolder),
                    ArtworkKind.FanArt => Path.Combine(FileSystemLayout.FanArtCacheFolder, subfolder),
                    _ => FileSystemLayout.LegacyImageCacheFolder
                };
                string target = Path.Combine(baseFolder, hex);
                Either<BaseError, Unit> maybeResult = await _localFileSystem.CopyFile(path, target);
                return maybeResult.Match<Either<BaseError, string>>(
                    _ => hex,
                    error => error);
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.ToString());
            }
        }

        public string GetPathForImage(string fileName, ArtworkKind artworkKind, Option<int> maybeMaxHeight)
        {
            string subfolder = maybeMaxHeight.Match(
                maxHeight => Path.Combine(maxHeight.ToString(), fileName[..2]),
                () => fileName[..2]);

            string baseFolder = artworkKind switch
            {
                ArtworkKind.Poster => Path.Combine(FileSystemLayout.PosterCacheFolder, subfolder),
                ArtworkKind.Thumbnail => Path.Combine(FileSystemLayout.ThumbnailCacheFolder, subfolder),
                ArtworkKind.Logo => Path.Combine(FileSystemLayout.LogoCacheFolder, subfolder),
                ArtworkKind.FanArt => Path.Combine(FileSystemLayout.FanArtCacheFolder, subfolder),
                ArtworkKind.Watermark => Path.Combine(FileSystemLayout.WatermarkCacheFolder, subfolder),
                _ => FileSystemLayout.LegacyImageCacheFolder
            };

            return Path.Combine(baseFolder, fileName);
        }

        public async Task<bool> IsAnimated(string fileName)
        {
            try
            {
                var cacheKey = $"image.animated.{Path.GetFileName(fileName)}";
                if (_memoryCache.TryGetValue(cacheKey, out bool animated))
                {
                    return animated;
                }

                using Image image = await Image.LoadAsync(fileName);
                animated = image.Frames.Count > 1;
                _memoryCache.Set(cacheKey, animated, TimeSpan.FromDays(1));

                return animated;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to check image for animation");
                return false;
            }
        }
    }
}
