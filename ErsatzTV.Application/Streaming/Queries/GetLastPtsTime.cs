using ErsatzTV.Core;

namespace ErsatzTV.Application.Streaming;

public record GetLastPtsTime(string ChannelNumber) : IRequest<Either<BaseError, PtsTime>>;
