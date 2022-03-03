using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.Configuration;

public record UpdateLibraryRefreshInterval(int LibraryRefreshInterval) : MediatR.IRequest<Either<BaseError, Unit>>;