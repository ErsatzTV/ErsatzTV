using ErsatzTV.Core.Api.FFmpegProfiles;

namespace ErsatzTV.Application.FFmpegProfiles;

public record GetFFmpegFullProfileByIdForApi(int Id) : IRequest<Option<FFmpegFullProfileResponseModel>>;
