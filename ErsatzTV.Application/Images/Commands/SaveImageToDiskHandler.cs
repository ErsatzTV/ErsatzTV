using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Images;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Images.Commands
{
    public class SaveImageToDiskHandler : IRequestHandler<SaveImageToDisk, Either<BaseError, string>>
    {
        private readonly IImageCache _imageCache;

        public SaveImageToDiskHandler(IImageCache imageCache) => _imageCache = imageCache;

        public Task<Either<BaseError, string>> Handle(
            SaveImageToDisk request,
            CancellationToken cancellationToken) => _imageCache.SaveImage(request.Buffer);
    }
}
