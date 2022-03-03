using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.FFmpegProfiles;

public record UpdateFFmpegSettings(FFmpegSettingsViewModel Settings) : MediatR.IRequest<Either<BaseError, Unit>>;