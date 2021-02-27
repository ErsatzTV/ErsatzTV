using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Images;
using LanguageExt;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace ErsatzTV.Infrastructure.Images
{
    public class ImageCache : IImageCache
    {
        private static readonly SHA1CryptoServiceProvider Crypto;

        static ImageCache() => Crypto = new SHA1CryptoServiceProvider();

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

        public async Task<Either<BaseError, string>> ResizeAndSaveImage(byte[] imageBuffer, int? height, int? width)
        {
            await using var inStream = new MemoryStream(imageBuffer);
            using var image = await Image.LoadAsync(inStream);

            Size size = height.HasValue ? new Size { Height = height.Value } : new Size { Width = width.Value };

            image.Mutate(
                i => i.Resize(
                    new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = size
                    }));

            await using var outStream = new MemoryStream();
            await image.SaveAsync(outStream, new JpegEncoder { Quality = 90 });

            return await SaveImage(outStream.ToArray());
        }

        public async Task<Either<BaseError, string>> SaveImage(byte[] imageBuffer)
        {
            try
            {
                byte[] hash = Crypto.ComputeHash(imageBuffer);
                string hex = BitConverter.ToString(hash).Replace("-", string.Empty);

                string fileName = Path.Combine(FileSystemLayout.ImageCacheFolder, hex);

                if (!Directory.Exists(FileSystemLayout.ImageCacheFolder))
                {
                    Directory.CreateDirectory(FileSystemLayout.ImageCacheFolder);
                }

                await File.WriteAllBytesAsync(fileName, imageBuffer);
                return hex;
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }
    }
}
