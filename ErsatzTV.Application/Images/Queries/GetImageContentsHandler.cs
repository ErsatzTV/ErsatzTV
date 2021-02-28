using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Images;
using LanguageExt;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Winista.Mime;

namespace ErsatzTV.Application.Images.Queries
{
    public class GetImageContentsHandler : IRequestHandler<GetImageContents, Either<BaseError, ImageViewModel>>
    {
        private static readonly MimeTypes MimeTypes = new();
        private readonly IImageCache _imageCache;
        private readonly IMemoryCache _memoryCache;

        public GetImageContentsHandler(IImageCache imageCache, IMemoryCache memoryCache)
        {
            _imageCache = imageCache;
            _memoryCache = memoryCache;
        }

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

                        string subfolder = request.FileName.Substring(0, 2);
                        string baseFolder = request.ArtworkKind switch
                        {
                            ArtworkKind.Poster => Path.Combine(FileSystemLayout.PosterCacheFolder, subfolder),
                            ArtworkKind.Thumbnail => Path.Combine(FileSystemLayout.ThumbnailCacheFolder, subfolder),
                            ArtworkKind.Logo => Path.Combine(FileSystemLayout.LogoCacheFolder, subfolder),
                            _ => FileSystemLayout.LegacyImageCacheFolder
                        };

                        string fileName = Path.Combine(baseFolder, request.FileName);
                        byte[] contents = await File.ReadAllBytesAsync(fileName, cancellationToken);

                        if (request.MaxHeight.HasValue)
                        {
                            Either<BaseError, byte[]> resizeResult = await _imageCache
                                .ResizeImage(contents, request.MaxHeight.Value);
                            resizeResult.IfRight(result => contents = result);
                        }

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
