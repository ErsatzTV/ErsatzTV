using MediatR;

namespace ErsatzTV.Application.FFmpegProfiles.Queries
{
    public record GetFFmpegSettings : IRequest<FFmpegSettingsViewModel>;
}
