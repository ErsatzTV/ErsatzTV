using ErsatzTV.Core;
using LanguageExt;
using MediatR;
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Application.Libraries;

public record MoveLocalLibraryPath(int LibraryPathId, int TargetLibraryId) : IRequest<Either<BaseError, Unit>>;