using ErsatzTV.Core;

namespace ErsatzTV.Application.Libraries;

public record CreateLocalLibraryPath
    (int LibraryId, string Path) : IRequest<Either<BaseError, LocalLibraryPathViewModel>>;