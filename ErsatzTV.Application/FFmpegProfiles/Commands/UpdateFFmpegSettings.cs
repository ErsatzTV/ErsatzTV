using ErsatzTV.Core;

namespace ErsatzTV.Application.FFmpegProfiles;

public record UpdateFFmpegSettings(FFmpegSettingsViewModel Settings) : MediatR.IRequest<Either<BaseError, Unit>>;