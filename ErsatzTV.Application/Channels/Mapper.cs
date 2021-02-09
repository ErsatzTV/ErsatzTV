using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Channels
{
    internal static class Mapper
    {
        internal static ChannelViewModel ProjectToViewModel(Channel channel) =>
            new(channel.Id, channel.Number, channel.Name, channel.FFmpegProfileId, channel.Logo, channel.StreamingMode);
    }
}
