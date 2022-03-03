namespace ErsatzTV.Application.FFmpegProfiles;

/// <summary>
///     Requests a new ffmpeg profile (view model) that contains
///     appropriate default values.
/// </summary>
public record NewFFmpegProfile : IRequest<FFmpegProfileViewModel>;