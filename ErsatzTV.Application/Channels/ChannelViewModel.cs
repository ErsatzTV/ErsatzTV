using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Channels
{
    public record ChannelViewModel(
        int Id,
        string Number,
        string Name,
        int FFmpegProfileId,
        string Logo,
        StreamingMode StreamingMode);
}
