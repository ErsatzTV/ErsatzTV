using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaSources;

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