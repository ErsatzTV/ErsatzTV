using ErsatzTV.Core.Api.FFmpegProfiles;

namespace ErsatzTV.Application.FFmpegProfiles;

public record GetAllFFmpegProfilesForApi : IRequest<List<FFmpegProfileResponseModel>>;
