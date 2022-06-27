using ErsatzTV.Core;

namespace ErsatzTV.Application.Configuration;

public record UpdateGeneralSettings(GeneralSettingsViewModel GeneralSettings) : IRequest<Either<BaseError, Unit>>;
