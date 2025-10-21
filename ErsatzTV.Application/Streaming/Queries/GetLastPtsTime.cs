using ErsatzTV.Core;
using ErsatzTV.Core.FFmpeg;

namespace ErsatzTV.Application.Streaming;

public record GetLastPtsTime(IHlsInitSegmentCache InitSegmentCache, string ChannelNumber)
    : IRequest<Either<BaseError, PtsTime>>;
