using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.Streaming.Commands
{
    public record StartFFmpegSession(string ChannelNumber) : MediatR.IRequest<Either<BaseError, Unit>>,
        IFFmpegWorkerRequest;
}
