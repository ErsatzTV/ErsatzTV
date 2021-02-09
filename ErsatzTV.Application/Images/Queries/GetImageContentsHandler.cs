using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using LanguageExt;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Winista.Mime;

namespace ErsatzTV.Application.Images.Queries
{
    public class GetImageContentsHandler : IRequestHandler<GetImageContents, Either<BaseError, ImageViewModel>>
    {
        private static readonly MimeTypes MimeTypes = new();
        private readonly IMemoryCache _memoryCache;

        public GetImageContentsHandler(IMemoryCache memoryCache) => _memoryCache = memoryCache;

        public async Task<Either<BaseError, ImageViewModel>> Handle(
            GetImageContents request,
            CancellationToken cancellationToken)
        {
            try
            {
                return await _memoryCache.GetOrCreateAsync(
                    request.FileName,
                    async entry =>
                    {
                        entry.SlidingExpiration = TimeSpan.FromHours(1);

                        string fileName = Path.Combine(FileSystemLayout.ImageCacheFolder, request.FileName);
                        byte[] contents = await File.ReadAllBytesAsync(fileName, cancellationToken);
                        MimeType mimeType = MimeTypes.GetMimeType(contents);
                        return new ImageViewModel(contents, mimeType.Name);
                    });
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }
    }
}
