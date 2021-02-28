using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.Libraries.Commands
{
    public record DeleteLocalLibraryPath(int LocalLibraryPathId) : MediatR.IRequest<Either<BaseError, Unit>>;
}
