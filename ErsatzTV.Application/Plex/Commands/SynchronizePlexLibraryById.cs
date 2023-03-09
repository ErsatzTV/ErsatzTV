using ErsatzTV.Core;

namespace ErsatzTV.Application.Plex;

public interface ISynchronizePlexLibraryById : IRequest<Either<BaseError, string>>, IScannerBackgroundServiceRequest
{
    int PlexLibraryId { get; }
    bool ForceScan { get; }
    bool DeepScan { get; }
}

public record SynchronizePlexLibraryByIdIfNeeded(int PlexLibraryId) : ISynchronizePlexLibraryById
{
    public bool ForceScan => false;
    public bool DeepScan => false;
}

public record ForceSynchronizePlexLibraryById(int PlexLibraryId, bool DeepScan) : ISynchronizePlexLibraryById
{
    public bool ForceScan => true;
}
