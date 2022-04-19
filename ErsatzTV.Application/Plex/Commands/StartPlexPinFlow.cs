using ErsatzTV.Core;

namespace ErsatzTV.Application.Plex;

public record StartPlexPinFlow : IRequest<Either<BaseError, string>>;
