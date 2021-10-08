using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.Streaming.Commands
{
    public record StartFFmpegSession(string ChannelNumber, bool StartAtZero) :
        MediatR.IRequest<Either<BaseError, Unit>>,
        IFFmpegWorkerRequest;
}
