using ErsatzTV.Core;

namespace ErsatzTV.Application.Emby;

public interface ISynchronizeEmbyShowByTitle : IRequest<Either<BaseError, string>>, IScannerBackgroundServiceRequest
{
    int EmbyLibraryId { get; }
    string ShowTitle { get; }
    bool DeepScan { get; }
}

public record SynchronizeEmbyShowByTitle(int EmbyLibraryId, string ShowTitle, bool DeepScan)
    : ISynchronizeEmbyShowByTitle;