using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;

namespace ErsatzTV.Application.Libraries.Commands
{
    public class
        DeleteLocalLibraryPathHandler : MediatR.IRequestHandler<DeleteLocalLibraryPath, Either<BaseError, Unit>>
    {
        private readonly ILibraryRepository _libraryRepository;

        public DeleteLocalLibraryPathHandler(ILibraryRepository libraryRepository) =>
            _libraryRepository = libraryRepository;

        public Task<Either<BaseError, Unit>> Handle(
            DeleteLocalLibraryPath request,
            CancellationToken cancellationToken) =>
            MediaSourceMustExist(request)
                .MapT(DoDeletion)
                .Bind(t => t.ToEitherAsync());

        private Task<Unit> DoDeletion(LibraryPath libraryPath) =>
            _libraryRepository.DeleteLocalPath(libraryPath.Id).ToUnit();

        private async Task<Validation<BaseError, LibraryPath>> MediaSourceMustExist(DeleteLocalLibraryPath request) =>
            (await _libraryRepository.GetPath(request.LocalLibraryPathId))
            .HeadOrNone()
            .ToValidation<BaseError>(
                $"Local library path {request.LocalLibraryPathId} does not exist.");
    }
}
