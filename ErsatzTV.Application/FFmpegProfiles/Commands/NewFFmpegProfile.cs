using MediatR;

namespace ErsatzTV.Application.FFmpegProfiles.Commands
{
    /// <summary>
    ///     Requests a new ffmpeg profile (view model) that contains
    ///     appropriate default values.
    /// </summary>
    public record NewFFmpegProfile : IRequest<FFmpegProfileViewModel>;
}
