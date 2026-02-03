using ErsatzTV.Core;

namespace ErsatzTV.Application.Configuration;

public record UpdateUiSettings(UiSettingsViewModel UiSettings) : IRequest<Either<BaseError, Unit>>;
