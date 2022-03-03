using ErsatzTV.Core;

namespace ErsatzTV.Application.Libraries;

public record DeleteLocalLibrary(int LocalLibraryId) : IRequest<Either<BaseError, Unit>>;