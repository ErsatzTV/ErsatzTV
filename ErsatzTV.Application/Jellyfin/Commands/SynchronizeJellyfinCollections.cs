using ErsatzTV.Core;

namespace ErsatzTV.Application.Jellyfin;

public record SynchronizeJellyfinCollections(int JellyfinMediaSourceId, bool ForceScan) :
    IRequest<Either<BaseError, Unit>>,
    IScannerBackgroundServiceRequest;
