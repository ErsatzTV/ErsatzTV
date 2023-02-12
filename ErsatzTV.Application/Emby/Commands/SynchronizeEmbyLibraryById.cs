using ErsatzTV.Core;

namespace ErsatzTV.Application.Emby;

public interface ISynchronizeEmbyLibraryById : IRequest<Either<BaseError, string>>, IEmbyBackgroundServiceRequest
{
    int EmbyLibraryId { get; }
    bool ForceScan { get; }
    bool DeepScan { get; }
}

public record SynchronizeEmbyLibraryByIdIfNeeded(int EmbyLibraryId) : ISynchronizeEmbyLibraryById
{
    public bool ForceScan => false;
    public bool DeepScan => false;
}

public record ForceSynchronizeEmbyLibraryById(int EmbyLibraryId, bool DeepScan) : ISynchronizeEmbyLibraryById
{
    public bool ForceScan => true;
}
