using ErsatzTV.Core;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Libraries;

public record CreateLocalLibrary(string Name, LibraryMediaKind MediaKind, List<string> Paths)
    : ILocalLibraryRequest, IRequest<Either<BaseError, LocalLibraryViewModel>>;
