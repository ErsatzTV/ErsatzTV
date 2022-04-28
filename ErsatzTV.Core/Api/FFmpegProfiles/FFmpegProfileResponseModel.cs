namespace ErsatzTV.Core.Api.FFmpegProfiles;

public record FFmpegProfileResponseModel(
    int Id,
    string Name,
    string Resolution,
    string Video,
    string Audio);
