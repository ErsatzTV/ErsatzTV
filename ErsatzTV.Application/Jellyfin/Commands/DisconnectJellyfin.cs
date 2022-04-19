using ErsatzTV.Core;

namespace ErsatzTV.Application.Jellyfin;

public record DisconnectJellyfin : IRequest<Either<BaseError, Unit>>;
