using ErsatzTV.Core;

namespace ErsatzTV.Application.Playouts;

public record UpdateSequentialPlayout(int PlayoutId, string ScheduleFile)
    : IRequest<Either<BaseError, PlayoutNameViewModel>>;
