using ErsatzTV.Core;

namespace ErsatzTV.Application.Playouts;

public record UpdateScriptedPlayout(int PlayoutId, string ScheduleFile)
    : IRequest<Either<BaseError, PlayoutNameViewModel>>;
