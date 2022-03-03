using ErsatzTV.Core;

namespace ErsatzTV.Application.Emby;

public record GetEmbyConnectionParameters : IRequest<Either<BaseError, EmbyConnectionParametersViewModel>>;