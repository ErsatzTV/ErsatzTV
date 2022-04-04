using System.Security.Cryptography;
using System.Text;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Images;
using ErsatzTV.Core.Interfaces.Metadata;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ErsatzTV.Infrastructure.Images;

public class ImageCache : IImageCache
{
    private static readonly SHA1 Crypto;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILogger<ImageCache> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly ITempFilePool _tempFilePool;

    static ImageCache() => Crypto = SHA1.Create();

    public ImageCache(
        ILocalFileSystem localFileSystem,
        IMemoryCache memoryCache,
        ITempFilePool tempFilePool,
        ILogger<ImageCache> logger)
    {
        _localFileSystem = localFileSystem;
        _memoryCache = memoryCache;
        _tempFilePool = tempFilePool;
        _logger = logger;
    }

    public async Task<Either<BaseError, string>> SaveArtworkToCache(Stream stream, ArtworkKind artworkKind)
    {
        try
        {
            string tempFileName = _tempFilePool.GetNextTempFile(TempFileCategory.CachedArtwork);
            // ReSharper disable once UseAwaitUsing
            using (var fs = new FileStream(tempFileName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                await stream.CopyToAsync(fs);
            }
            byte[] hash = await ComputeFileHash(tempFileName);
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

            await _localFileSystem.CopyFile(tempFileName, target);

            return hex;
        }
        catch (Exception ex)
        {
            return BaseError.New(ex.Message);
        }
    }

    private static async Task<byte[]> ComputeFileHash(string fileName)
    {
        using var md5 = MD5.Create();
        // ReSharper disable once UseAwaitUsing
        // ReSharper disable once ConvertToUsingDeclaration
        using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
        {
            fs.Position = 0;
            return await md5.ComputeHashAsync(fs);
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

    public async Task<string> CalculateBlurHash(string fileName, ArtworkKind artworkKind, int x, int y)
    {
        var encoder = new Blurhash.ImageSharp.Encoder();
        string targetFile = GetPathForImage(fileName, artworkKind, Option<int>.None);
        await using var fs = new FileStream(targetFile, FileMode.Open, FileAccess.Read);
        using var image = await Image.LoadAsync<Rgb24>(fs);
        return encoder.Encode(image, x, y);
    }

    public async Task<string> WriteBlurHash(string blurHash, IDisplaySize targetSize)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(blurHash);
        string base64 = Convert.ToBase64String(bytes).Replace("+", "_").Replace("/", "-").Replace("=", "");
        string targetFile = GetPathForImage(base64, ArtworkKind.Poster, targetSize.Height);
        if (!_localFileSystem.FileExists(targetFile))
        {
            string folder = Path.GetDirectoryName(targetFile);
            _localFileSystem.EnsureFolderExists(folder);
                
            var decoder = new Blurhash.ImageSharp.Decoder();
            using Image<Rgb24> image = decoder.Decode(blurHash, targetSize.Width, targetSize.Height);
            await image.SaveAsPngAsync(targetFile);
        }

        return targetFile;
    }
}