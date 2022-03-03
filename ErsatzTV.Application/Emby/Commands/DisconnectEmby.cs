using ErsatzTV.Core;

namespace ErsatzTV.Application.Emby;

public record DisconnectEmby : MediatR.IRequest<Either<BaseError, Unit>>;