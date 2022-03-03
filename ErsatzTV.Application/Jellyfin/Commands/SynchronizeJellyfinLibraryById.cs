using ErsatzTV.Core;

namespace ErsatzTV.Application.Jellyfin;

public interface ISynchronizeJellyfinLibraryById : IRequest<Either<BaseError, string>>,
    IJellyfinBackgroundServiceRequest
{
    int JellyfinLibraryId { get; }
    bool ForceScan { get; }
}

public record SynchronizeJellyfinLibraryByIdIfNeeded(int JellyfinLibraryId) : ISynchronizeJellyfinLibraryById
{
    public bool ForceScan => false;
}

public record ForceSynchronizeJellyfinLibraryById(int JellyfinLibraryId) : ISynchronizeJellyfinLibraryById
{
    public bool ForceScan => true;
}