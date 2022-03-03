using ErsatzTV.Core;

namespace ErsatzTV.Application.Playouts;

public record UpdatePlayout
    (int PlayoutId, Option<TimeSpan> DailyRebuildTime) : IRequest<Either<BaseError, PlayoutNameViewModel>>;