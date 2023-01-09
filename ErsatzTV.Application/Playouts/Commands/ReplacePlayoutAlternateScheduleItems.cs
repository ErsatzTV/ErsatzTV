using ErsatzTV.Core;

namespace ErsatzTV.Application.Playouts;

public record ReplacePlayoutAlternateScheduleItems
    (int PlayoutId, List<ReplacePlayoutAlternateSchedule> Items) : IRequest<Either<BaseError, Unit>>;
