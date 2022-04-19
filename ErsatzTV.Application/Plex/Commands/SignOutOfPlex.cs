using ErsatzTV.Core;

namespace ErsatzTV.Application.Plex;

public record SignOutOfPlex : IRequest<Either<BaseError, Unit>>;
