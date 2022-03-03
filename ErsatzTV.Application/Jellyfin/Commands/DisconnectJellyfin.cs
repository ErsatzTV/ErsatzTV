using ErsatzTV.Core;

namespace ErsatzTV.Application.Jellyfin;

public record DisconnectJellyfin : MediatR.IRequest<Either<BaseError, Unit>>;