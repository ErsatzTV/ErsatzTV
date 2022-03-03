using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.Streaming;

public record StartFFmpegSession(string ChannelNumber, bool StartAtZero) :
    MediatR.IRequest<Either<BaseError, Unit>>,
    IFFmpegWorkerRequest;