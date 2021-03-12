using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.FFmpegProfiles.Commands
{
    public record UpdateFFmpegSettings(FFmpegSettingsViewModel Settings) : MediatR.IRequest<Either<BaseError, Unit>>;
}
