using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaSources.Commands
{
    public record ScanLocalMediaSource(int MediaSourceId) : IRequest<Either<Seq<BaseError>, string>>,
        IBackgroundServiceRequest;
}
