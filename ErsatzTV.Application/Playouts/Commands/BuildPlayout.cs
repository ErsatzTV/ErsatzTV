using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.Playouts.Commands
{
    public record BuildPlayout(int PlayoutId, bool Rebuild = false) : MediatR.IRequest<Either<BaseError, Unit>>,
        IBackgroundServiceRequest;
}
