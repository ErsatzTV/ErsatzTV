using ErsatzTV.Core;

namespace ErsatzTV.Application.Configuration;

public record UpdateLoggingSettings(LoggingSettingsViewModel LoggingSettings) : IRequest<Either<BaseError, Unit>>;
