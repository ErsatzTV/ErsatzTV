using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Text;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Images;
using ErsatzTV.Core.Interfaces.Metadata;
using Decoder = System.Drawing.Common.Blurhash.Decoder;
using Encoder = System.Drawing.Common.Blurhash.Encoder;

namespace ErsatzTV.Infrastructure.Images;

public class ImageCache : IImageCache
{
    private static readonly SHA1 Crypto;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ITempFilePool _tempFilePool;

    static ImageCache() => Crypto = SHA1.Create();

    public ImageCache(
        ILocalFileSystem localFileSystem,
        ITempFilePool tempFilePool)
    {
        _localFileSystem = localFileSystem;
        _tempFilePool = tempFilePool;
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

    public async Task<Either<BaseError, string>> CopyArtworkToCache(string path, ArtworkKind artworkKind)
    {
        try
        {
            string filenameKey = $"{path}:{_localFileSystem.GetLastWriteTime(path).ToFileTimeUtc()}";
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

    public string CalculateBlurHash(string fileName, ArtworkKind artworkKind, int x, int y)
    {
        var encoder = new Encoder();
        string targetFile = GetPathForImage(fileName, artworkKind, Option<int>.None);
        // ReSharper disable once ConvertToUsingDeclaration
        using (var image = Image.FromFile(targetFile))
        {
            return encoder.Encode(image, x, y);
        }
    }

    public string WriteBlurHash(string blurHash, IDisplaySize targetSize)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(blurHash);
        string base64 = Convert.ToBase64String(bytes).Replace("+", "_").Replace("/", "-").Replace("=", "");
        string targetFile = GetPathForImage(base64, ArtworkKind.Poster, targetSize.Height);
        if (!_localFileSystem.FileExists(targetFile))
        {
            string folder = Path.GetDirectoryName(targetFile);
            _localFileSystem.EnsureFolderExists(folder);

            var decoder = new Decoder();
            // ReSharper disable once ConvertToUsingDeclaration
            using (Image image = decoder.Decode(blurHash, targetSize.Width, targetSize.Height))
            {
                image.Save(targetFile, ImageFormat.Png);
            }
        }

        return targetFile;
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
}
