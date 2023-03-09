using ErsatzTV.Core;

namespace ErsatzTV.Application.Jellyfin;

public record SynchronizeJellyfinAdminUserId(int JellyfinMediaSourceId) : IRequest<Either<BaseError, Unit>>,
    IScannerBackgroundServiceRequest;
