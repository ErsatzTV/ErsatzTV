using ErsatzTV.Core;

namespace ErsatzTV.Application.Plex;

public record SignOutOfPlex : MediatR.IRequest<Either<BaseError, Unit>>;