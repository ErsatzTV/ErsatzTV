using ErsatzTV.Core;
using LanguageExt;
using MediatR;
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Application.Playouts.Commands
{
    public record DeletePlayout(int PlayoutId) : IRequest<Either<BaseError, Unit>>;
}
