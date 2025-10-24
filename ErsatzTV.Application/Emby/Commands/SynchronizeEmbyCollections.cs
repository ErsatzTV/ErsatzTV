using ErsatzTV.Core;

namespace ErsatzTV.Application.Emby;

public record SynchronizeEmbyCollections(int EmbyMediaSourceId, bool ForceScan, bool DeepScan)
    : IRequest<Either<BaseError, Unit>>,
        IScannerBackgroundServiceRequest;
