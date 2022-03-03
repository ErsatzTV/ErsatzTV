using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Images;
using LanguageExt;
using MediatR;
using Winista.Mime;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.Images;

public class
    GetCachedImagePathHandler : IRequestHandler<GetCachedImagePath, Either<BaseError, CachedImagePathViewModel>>
{
    private static readonly MimeTypes MimeTypes = new();
    private readonly IImageCache _imageCache;

    public GetCachedImagePathHandler(IImageCache imageCache) => _imageCache = imageCache;

    public async Task<Either<BaseError, CachedImagePathViewModel>> Handle(
        GetCachedImagePath request,
        CancellationToken cancellationToken)
    {
        try
        {
            MimeType mimeType;

            string cachePath = _imageCache.GetPathForImage(
                request.FileName,
                request.ArtworkKind,
                Optional(request.MaxHeight));
            if (!File.Exists(cachePath))
            {
                if (request.MaxHeight.HasValue)
                {
                    string originalPath = _imageCache.GetPathForImage(request.FileName, request.ArtworkKind, None);
                    byte[] contents = await File.ReadAllBytesAsync(originalPath, cancellationToken);
                    Either<BaseError, byte[]> resizeResult =
                        await _imageCache.ResizeImage(contents, request.MaxHeight.Value);
                    resizeResult.IfRight(result => contents = result);

                    string baseFolder = Path.GetDirectoryName(cachePath);
                    if (baseFolder != null && !Directory.Exists(baseFolder))
                    {
                        Directory.CreateDirectory(baseFolder);
                    }

                    await File.WriteAllBytesAsync(cachePath, contents, cancellationToken);

                    mimeType = new MimeType("image/jpeg");
                }
                else
                {
                    return BaseError.New($"Artwork does not exist on disk at {cachePath}");
                }
            }
            else
            {
                mimeType = MimeTypes.GetMimeTypeFromFile(cachePath);
            }

            return new CachedImagePathViewModel(cachePath, mimeType.Name);
        }
        catch (Exception ex)
        {
            return BaseError.New(ex.Message);
        }
    }
}