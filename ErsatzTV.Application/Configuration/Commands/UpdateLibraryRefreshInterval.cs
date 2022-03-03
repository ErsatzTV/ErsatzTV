using ErsatzTV.Core;

namespace ErsatzTV.Application.Configuration;

public record UpdateLibraryRefreshInterval(int LibraryRefreshInterval) : MediatR.IRequest<Either<BaseError, Unit>>;