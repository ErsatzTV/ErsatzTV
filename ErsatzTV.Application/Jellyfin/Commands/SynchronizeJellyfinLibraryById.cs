using ErsatzTV.Core;

namespace ErsatzTV.Application.Jellyfin;

public interface ISynchronizeJellyfinLibraryById : IRequest<Either<BaseError, string>>,
    IJellyfinBackgroundServiceRequest
{
    int JellyfinLibraryId { get; }
    bool ForceScan { get; }
    bool DeepScan { get; }
}

public record SynchronizeJellyfinLibraryByIdIfNeeded(int JellyfinLibraryId) : ISynchronizeJellyfinLibraryById
{
    public bool ForceScan => false;
    public bool DeepScan => false;
}

public record ForceSynchronizeJellyfinLibraryById
    (int JellyfinLibraryId, bool DeepScan) : ISynchronizeJellyfinLibraryById
{
    public bool ForceScan => true;
}
