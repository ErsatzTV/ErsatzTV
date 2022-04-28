using ErsatzTV.Core;

namespace ErsatzTV.Application.FFmpegProfiles;

public record UpdateFFmpegSettings(FFmpegSettingsViewModel Settings) : IRequest<Either<BaseError, Unit>>;
