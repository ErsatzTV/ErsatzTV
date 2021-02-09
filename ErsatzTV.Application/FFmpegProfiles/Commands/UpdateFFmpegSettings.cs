using MediatR;

namespace ErsatzTV.Application.FFmpegProfiles.Commands
{
    public record UpdateFFmpegSettings(FFmpegSettingsViewModel Settings) : IRequest;
}
