using ErsatzTV.Core;

namespace ErsatzTV.Application.Playouts;

public record UpdateExternalJsonPlayout(int PlayoutId, string ScheduleFile)
    : IRequest<Either<BaseError, PlayoutNameViewModel>>;
