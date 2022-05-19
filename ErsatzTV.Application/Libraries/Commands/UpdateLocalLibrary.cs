using ErsatzTV.Core;

namespace ErsatzTV.Application.Libraries;

public record UpdateLocalLibraryPath(int Id, string Path);

public record UpdateLocalLibrary(int Id, string Name, List<UpdateLocalLibraryPath> Paths) : ILocalLibraryRequest,
    IRequest<Either<BaseError, LocalLibraryViewModel>>;
