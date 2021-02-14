using ErsatzTV.Core;
using ErsatzTV.Core.Metadata;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaSources.Commands
{
    public record ScanLocalMediaSource(int MediaSourceId, ScanningMode ScanningMode) :
        IRequest<Either<BaseError, string>>,
        IBackgroundServiceRequest;
}
