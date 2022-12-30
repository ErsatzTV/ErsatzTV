using ErsatzTV.Core;

namespace ErsatzTV.Scanner.Application.Jellyfin;

public record SynchronizeJellyfinAdminUserId(int JellyfinMediaSourceId) : IRequest<Either<BaseError, Unit>>;
