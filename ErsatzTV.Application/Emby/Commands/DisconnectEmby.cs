using ErsatzTV.Core;

namespace ErsatzTV.Application.Emby;

public record DisconnectEmby : IRequest<Either<BaseError, Unit>>;
