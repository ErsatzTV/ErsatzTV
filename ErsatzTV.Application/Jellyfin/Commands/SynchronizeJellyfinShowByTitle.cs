using ErsatzTV.Core;

namespace ErsatzTV.Application.Jellyfin;

public interface ISynchronizeJellyfinShowByTitle : IRequest<Either<BaseError, string>>, IScannerBackgroundServiceRequest
{
    int JellyfinLibraryId { get; }
    string ShowTitle { get; }
    bool DeepScan { get; }
}

public record SynchronizeJellyfinShowByTitle(int JellyfinLibraryId, string ShowTitle, bool DeepScan)
    : ISynchronizeJellyfinShowByTitle;