using ErsatzTV.Core;

namespace ErsatzTV.Application.Playouts;

public record TimeShiftOnDemandPlayout(int PlayoutId, DateTimeOffset Now, bool Force)
    : IRequest<Option<BaseError>>, IBackgroundServiceRequest;
