using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Images.Commands
{
    public class SaveImageToDiskHandler : IRequestHandler<SaveImageToDisk, Either<BaseError, string>>
    {
        private static readonly SHA1CryptoServiceProvider Crypto;

        static SaveImageToDiskHandler() => Crypto = new SHA1CryptoServiceProvider();

        public async Task<Either<BaseError, string>> Handle(
            SaveImageToDisk request,
            CancellationToken cancellationToken)
        {
            try
            {
                byte[] hash = Crypto.ComputeHash(request.Buffer);
                string hex = BitConverter.ToString(hash).Replace("-", string.Empty);

                string fileName = Path.Combine(FileSystemLayout.ImageCacheFolder, hex);

                if (!Directory.Exists(FileSystemLayout.ImageCacheFolder))
                {
                    Directory.CreateDirectory(FileSystemLayout.ImageCacheFolder);
                }

                await File.WriteAllBytesAsync(fileName, request.Buffer, cancellationToken);
                return hex;
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }
    }
}
