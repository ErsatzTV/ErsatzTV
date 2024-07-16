using ErsatzTV.Core;

namespace ErsatzTV.Application.Playouts;

public record TimeShiftOnDemandPlayout(string ChannelNumber, DateTimeOffset Now, bool Force)
    : IRequest<Option<BaseError>>, IBackgroundServiceRequest;
