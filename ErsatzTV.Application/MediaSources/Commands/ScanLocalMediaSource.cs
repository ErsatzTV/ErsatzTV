using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaSources.Commands
{
    public record ScanLocalMediaSource(int MediaSourceId, bool RefreshAllMetadata) : IRequest<Either<BaseError, string>>,
        IBackgroundServiceRequest;
}
