using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.Configuration;

public record UpdatePaginationSettings(PaginationSettingsViewModel Settings) : IRequest<Either<BaseError, Unit>>;
