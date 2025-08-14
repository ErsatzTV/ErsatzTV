using ErsatzTV.Core;

namespace ErsatzTV.Application.Emby;

public record SynchronizeEmbyShowById(int EmbyLibraryId, int ShowId, bool DeepScan)
    : IRequest<Either<BaseError, string>>, IScannerBackgroundServiceRequest;
