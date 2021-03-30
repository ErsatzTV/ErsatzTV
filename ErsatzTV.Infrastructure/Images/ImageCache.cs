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
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace ErsatzTV.Infrastructure.Images
{
    public class ImageCache : IImageCache
    {
        private static readonly SHA1CryptoServiceProvider Crypto;
        private readonly ILocalFileSystem _localFileSystem;
        private readonly ILogger<ImageCache> _logger;

        static ImageCache() => Crypto = new SHA1CryptoServiceProvider();

        public ImageCache(ILocalFileSystem localFileSystem, ILogger<ImageCache> logger)
        {
            _localFileSystem = localFileSystem;
            _logger = logger;
        }

        public async Task<Either<BaseError, byte[]>> ResizeImage(byte[] imageBuffer, int height)
        {
            await using var inStream = new MemoryStream(imageBuffer);
            using var image = await Image.LoadAsync(inStream);

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
                string subfolder = hex.Substring(0, 2);
                string baseFolder = artworkKind switch
                {
                    ArtworkKind.Poster => Path.Combine(FileSystemLayout.PosterCacheFolder, subfolder),
                    ArtworkKind.Thumbnail => Path.Combine(FileSystemLayout.ThumbnailCacheFolder, subfolder),
                    ArtworkKind.Logo => Path.Combine(FileSystemLayout.LogoCacheFolder, subfolder),
                    ArtworkKind.FanArt => Path.Combine(FileSystemLayout.FanArtCacheFolder, subfolder),
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
                string subfolder = hex.Substring(0, 2);
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
    }
}
