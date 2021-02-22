using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaSources.Commands
{
    public interface IScanLocalMediaSource : IRequest<Either<BaseError, string>>, IBackgroundServiceRequest
    {
        int MediaSourceId { get; }
        bool ForceScan { get; }
    }

    public record ScanLocalMediaSourceIfNeeded(int MediaSourceId) : IScanLocalMediaSource
    {
        public bool ForceScan => false;
    }

    public record ForceScanLocalMediaSource(int MediaSourceId) : IScanLocalMediaSource
    {
        public bool ForceScan => true;
    }
}
