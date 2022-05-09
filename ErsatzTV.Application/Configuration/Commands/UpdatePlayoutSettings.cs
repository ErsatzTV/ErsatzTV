using ErsatzTV.Core;

namespace ErsatzTV.Application.Configuration;

public record UpdatePlayoutSettings(PlayoutSettingsViewModel PlayoutSettings) : IRequest<Either<BaseError, Unit>>;
