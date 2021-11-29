using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Images;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Images.Commands
{
    public class SaveArtworkToDiskHandler : IRequestHandler<SaveArtworkToDisk, Either<BaseError, string>>
    {
        private readonly IImageCache _imageCache;

        public SaveArtworkToDiskHandler(IImageCache imageCache) => _imageCache = imageCache;

        public Task<Either<BaseError, string>> Handle(SaveArtworkToDisk request, CancellationToken cancellationToken) =>
            _imageCache.SaveArtworkToCache(request.Stream, request.ArtworkKind);
    }
}
