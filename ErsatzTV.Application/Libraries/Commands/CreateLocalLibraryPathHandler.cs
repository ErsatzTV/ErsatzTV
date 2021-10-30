using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static LanguageExt.Prelude;
using static ErsatzTV.Application.Libraries.Mapper;

namespace ErsatzTV.Application.Libraries.Commands
{
    public class CreateLocalLibraryPathHandler : IRequestHandler<CreateLocalLibraryPath,
        Either<BaseError, LocalLibraryPathViewModel>>
    {
        private readonly ILibraryRepository _libraryRepository;

        public CreateLocalLibraryPathHandler(ILibraryRepository libraryRepository) =>
            _libraryRepository = libraryRepository;

        public Task<Either<BaseError, LocalLibraryPathViewModel>> Handle(
            CreateLocalLibraryPath request,
            CancellationToken cancellationToken) =>
            Validate(request).MapT(PersistLocalLibraryPath).Bind(v => v.ToEitherAsync());

        private Task<LocalLibraryPathViewModel> PersistLocalLibraryPath(LibraryPath p) =>
            _libraryRepository.Add(p).Map(ProjectToViewModel);

        private Task<Validation<BaseError, LibraryPath>> Validate(CreateLocalLibraryPath request) =>
            ValidateFolder(request)
                .MapT(
                    folder =>
                        new LibraryPath
                        {
                            LibraryId = request.LibraryId,
                            Path = folder
                        });

        private async Task<Validation<BaseError, string>> ValidateFolder(CreateLocalLibraryPath request)
        {
            List<string> allPaths = await _libraryRepository.GetLocalPaths(request.LibraryId)
                .Map(list => list.Map(c => c.Path).ToList());

            return Optional(request.Path)
                .Where(folder => allPaths.ForAll(f => !AreSubPaths(f, folder)))
                .ToValidation<BaseError>("Path must not belong to another library path");
        }

        private static bool AreSubPaths(string path1, string path2)
        {
            string one = path1 + Path.DirectorySeparatorChar;
            string two = path2 + Path.DirectorySeparatorChar;
            return one == two || one.StartsWith(two, StringComparison.OrdinalIgnoreCase) ||
                   two.StartsWith(one, StringComparison.OrdinalIgnoreCase);
        }
    }
}
