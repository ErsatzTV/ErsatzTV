using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Images;

namespace ErsatzTV.Application.Images;

public class SaveArtworkToDiskHandler : IRequestHandler<SaveArtworkToDisk, Either<BaseError, string>>
{
    private readonly IImageCache _imageCache;

    public SaveArtworkToDiskHandler(IImageCache imageCache) => _imageCache = imageCache;

    public Task<Either<BaseError, string>> Handle(SaveArtworkToDisk request, CancellationToken cancellationToken) =>
        _imageCache.SaveArtworkToCache(request.Stream, request.ArtworkKind);
}