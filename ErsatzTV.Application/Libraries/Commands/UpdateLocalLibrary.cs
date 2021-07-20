using System.Collections.Generic;
using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Libraries.Commands
{
    public record UpdateLocalLibraryPath(int Id, string Path);

    public record UpdateLocalLibrary(int Id, string Name, List<UpdateLocalLibraryPath> Paths) : ILocalLibraryRequest,
        IRequest<Either<BaseError, LocalLibraryViewModel>>;
}
