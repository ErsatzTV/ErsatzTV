using ErsatzTV.Core;

namespace ErsatzTV.Application.Emby;

public record SynchronizeEmbyCollections(int EmbyMediaSourceId, bool ForceScan) : IRequest<Either<BaseError, Unit>>,
    IScannerBackgroundServiceRequest;
