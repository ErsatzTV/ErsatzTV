using ErsatzTV.Core;

namespace ErsatzTV.Application.Jellyfin;

public record GetJellyfinConnectionParameters : IRequest<Either<BaseError, JellyfinConnectionParametersViewModel>>;
