namespace ErsatzTV.Core.Api.Channels;

public record ChannelResponseModel(
    int Id,
    string Number,
    string Name,
    string FFmpegProfile,
    string Language,
    string StreamingMode);
