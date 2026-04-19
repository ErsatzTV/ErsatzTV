using ErsatzTV.Core;

namespace ErsatzTV.Application.Streaming;

public record StartFFmpegNextSession(
    string ChannelNumber,
    string Mode,
    string Scheme,
    string Host,
    string PathBase,
    string AccessTokenQuery) :
    IRequest<Either<BaseError, string>>,
    IFFmpegWorkerRequest;
