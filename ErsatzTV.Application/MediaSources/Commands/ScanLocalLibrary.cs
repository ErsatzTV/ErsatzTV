using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaSources.Commands
{
    public interface IScanLocalLibrary : IRequest<Either<BaseError, string>>, IBackgroundServiceRequest
    {
        int LibraryId { get; }
        bool ForceScan { get; }
        bool Rescan { get; }
    }

    public record ScanLocalLibraryIfNeeded(int LibraryId) : IScanLocalLibrary
    {
        public bool ForceScan => false;
        public bool Rescan => false;
    }

    public record ForceScanLocalLibrary(int LibraryId) : IScanLocalLibrary
    {
        public bool ForceScan => true;
        public bool Rescan => false;
    }

    public record ForceRescanLocalLibrary(int LibraryId) : IScanLocalLibrary
    {
        public bool ForceScan => true;
        public bool Rescan => true;
    }
}
