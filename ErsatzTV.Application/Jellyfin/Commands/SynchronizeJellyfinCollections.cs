using ErsatzTV.Core;

namespace ErsatzTV.Application.Jellyfin;

public record SynchronizeJellyfinCollections(int JellyfinMediaSourceId, bool ForceScan, bool DeepScan) :
    IRequest<Either<BaseError, Unit>>,
    IScannerBackgroundServiceRequest;
