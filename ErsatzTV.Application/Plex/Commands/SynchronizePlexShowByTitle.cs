using ErsatzTV.Core;

namespace ErsatzTV.Application.Plex;

public interface ISynchronizePlexShowByTitle : IRequest<Either<BaseError, string>>, IScannerBackgroundServiceRequest
{
    int PlexLibraryId { get; }
    string ShowTitle { get; }
    bool DeepScan { get; }
}

public record SynchronizePlexShowByTitle(int PlexLibraryId, string ShowTitle, bool DeepScan)
    : ISynchronizePlexShowByTitle;