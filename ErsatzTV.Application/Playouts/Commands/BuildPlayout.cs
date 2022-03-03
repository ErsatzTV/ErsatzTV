using ErsatzTV.Core;

namespace ErsatzTV.Application.Playouts;

public record BuildPlayout(int PlayoutId, bool Rebuild = false) : MediatR.IRequest<Either<BaseError, Unit>>,
    IBackgroundServiceRequest;