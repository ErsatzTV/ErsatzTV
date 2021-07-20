using ErsatzTV.Core;
using LanguageExt;
using MediatR;
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Application.Libraries.Commands
{
    public record DeleteLocalLibrary(int LocalLibraryId) : IRequest<Either<BaseError, Unit>>;
}
