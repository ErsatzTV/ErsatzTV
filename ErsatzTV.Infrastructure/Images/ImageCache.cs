﻿using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Blurhash.ImageSharp;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Images;
using ErsatzTV.Core.Interfaces.Metadata;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;

namespace ErsatzTV.Infrastructure.Images;

[SuppressMessage("Security", "CA5350:Do Not Use Weak Cryptographic Algorithms")]
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
                // overwrite anything that's already there
                fs.SetLength(0);

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

    public virtual string GetPathForImage(string fileName, ArtworkKind artworkKind, Option<int> maybeMaxHeight)
    {
        string subfolder = maybeMaxHeight.Match(
            maxHeight => Path.Combine(maxHeight.ToString(CultureInfo.InvariantCulture), fileName[..2]),
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

    public async Task<string> CalculateBlurHash(string fileName, ArtworkKind artworkKind, int x, int y)
    {
        string targetFile = GetPathForImage(fileName, artworkKind, Option<int>.None);

        // ReSharper disable once ConvertToUsingDeclaration
        using (var image = await Image.LoadAsync<Rgba32>(targetFile))
        {
            // resize before calculating blur hash; it doesn't need giant images
            if (image.Height > 200)
            {
                image.Mutate(i => i.Resize(0, 200));
            }
            else if (image.Width > 200)
            {
                image.Mutate(i => i.Resize(200, 0));
            }

            return Blurhasher.Encode(image, x, y);
        }
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

            // ReSharper disable once ConvertToUsingDeclaration
            // ReSharper disable once UseAwaitUsing
            using (FileStream fs = File.OpenWrite(targetFile))
            {
                using (Image<Rgb24> image = Blurhasher.Decode(blurHash, targetSize.Width, targetSize.Height))
                {
                    await image.SaveAsPngAsync(fs);
                }
            }
        }

        return targetFile;
    }

    [SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms")]
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
