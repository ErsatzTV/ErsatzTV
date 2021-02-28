using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Libraries.Commands
{
    public record CreateLocalLibraryPath
        (int LibraryId, string Path) : IRequest<Either<BaseError, LocalLibraryPathViewModel>>;
}
