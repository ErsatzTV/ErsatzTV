using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaSources.Commands
{
    public interface IScanLocalLibrary : IRequest<Either<BaseError, string>>, IBackgroundServiceRequest
    {
        int LibraryId { get; }
        bool ForceScan { get; }
    }

    public record ScanLocalLibraryIfNeeded(int LibraryId) : IScanLocalLibrary
    {
        public bool ForceScan => false;
    }

    public record ForceScanLocalLibrary(int LibraryId) : IScanLocalLibrary
    {
        public bool ForceScan => true;
    }
}
