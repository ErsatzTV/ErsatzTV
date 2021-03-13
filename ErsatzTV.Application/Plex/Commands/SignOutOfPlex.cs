using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.Plex.Commands
{
    public record SignOutOfPlex : MediatR.IRequest<Either<BaseError, Unit>>;
}
