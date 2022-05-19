using ErsatzTV.Core;

namespace ErsatzTV.Application.Streaming;

public record GetLastPtsDuration(string ChannelNumber) : IRequest<Either<BaseError, PtsAndDuration>>;
