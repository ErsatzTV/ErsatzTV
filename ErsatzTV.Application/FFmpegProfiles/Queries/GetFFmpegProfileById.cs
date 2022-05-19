namespace ErsatzTV.Application.FFmpegProfiles;

public record GetFFmpegProfileById(int Id) : IRequest<Option<FFmpegProfileViewModel>>;
