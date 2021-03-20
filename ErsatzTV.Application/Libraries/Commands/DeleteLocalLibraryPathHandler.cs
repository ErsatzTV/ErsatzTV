using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using LanguageExt;

namespace ErsatzTV.Application.Libraries.Commands
{
    public class
        DeleteLocalLibraryPathHandler : MediatR.IRequestHandler<DeleteLocalLibraryPath, Either<BaseError, Unit>>
    {
        private readonly ILibraryRepository _libraryRepository;
        private readonly ISearchIndex _searchIndex;

        public DeleteLocalLibraryPathHandler(ILibraryRepository libraryRepository, ISearchIndex searchIndex)
        {
            _libraryRepository = libraryRepository;
            _searchIndex = searchIndex;
        }

        public Task<Either<BaseError, Unit>> Handle(
            DeleteLocalLibraryPath request,
            CancellationToken cancellationToken) =>
            MediaSourceMustExist(request)
                .MapT(DoDeletion)
                .Bind(t => t.ToEitherAsync());

        private async Task<Unit> DoDeletion(LibraryPath libraryPath)
        {
            List<int> ids = await _libraryRepository.GetMediaIdsByLocalPath(libraryPath.Id);
            await _searchIndex.RemoveItems(ids);
            await _libraryRepository.DeleteLocalPath(libraryPath.Id);
            return Unit.Default;
        }

        private async Task<Validation<BaseError, LibraryPath>> MediaSourceMustExist(DeleteLocalLibraryPath request) =>
            (await _libraryRepository.GetPath(request.LocalLibraryPathId))
            .HeadOrNone()
            .ToValidation<BaseError>(
                $"Local library path {request.LocalLibraryPathId} does not exist.");
    }
}
