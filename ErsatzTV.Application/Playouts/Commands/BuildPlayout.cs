using ErsatzTV.Core;
using ErsatzTV.Core.Scheduling;

namespace ErsatzTV.Application.Playouts;

public record BuildPlayout(int PlayoutId, PlayoutBuildMode Mode) : IRequest<Either<BaseError, Unit>>,
    IBackgroundServiceRequest;