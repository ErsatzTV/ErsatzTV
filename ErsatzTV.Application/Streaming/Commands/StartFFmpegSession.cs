using ErsatzTV.Core;

namespace ErsatzTV.Application.Streaming;

public record StartFFmpegSession(string ChannelNumber, string Mode, string Scheme, string Host) :
    IRequest<Either<BaseError, Unit>>,
    IFFmpegWorkerRequest;
