using ErsatzTV.Core;

namespace ErsatzTV.Application.Configuration;

public record UpdateLibraryRefreshInterval(int LibraryRefreshInterval) : IRequest<Either<BaseError, Unit>>;
