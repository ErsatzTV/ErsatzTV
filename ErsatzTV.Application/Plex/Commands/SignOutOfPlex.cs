using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.Plex;

public record SignOutOfPlex : MediatR.IRequest<Either<BaseError, Unit>>;