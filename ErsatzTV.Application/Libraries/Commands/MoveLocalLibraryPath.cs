using ErsatzTV.Core;

namespace ErsatzTV.Application.Libraries;

public record MoveLocalLibraryPath(int LibraryPathId, int TargetLibraryId) : IRequest<Either<BaseError, Unit>>;
