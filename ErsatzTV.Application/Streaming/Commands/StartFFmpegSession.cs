using ErsatzTV.Core;

namespace ErsatzTV.Application.Streaming;

public record StartFFmpegSession(string ChannelNumber, bool StartAtZero) :
    IRequest<Either<BaseError, Unit>>,
    IFFmpegWorkerRequest;
