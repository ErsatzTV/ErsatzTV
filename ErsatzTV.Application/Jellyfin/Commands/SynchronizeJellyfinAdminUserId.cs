using ErsatzTV.Core;

namespace ErsatzTV.Application.Jellyfin;

public record SynchronizeJellyfinAdminUserId(int JellyfinMediaSourceId) : MediatR.IRequest<Either<BaseError, Unit>>,
    IJellyfinBackgroundServiceRequest;