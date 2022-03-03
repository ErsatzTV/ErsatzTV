using MediatR;

namespace ErsatzTV.Application.FFmpegProfiles;

public record GetFFmpegSettings : IRequest<FFmpegSettingsViewModel>;