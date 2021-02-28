using System.Linq;
using ErsatzTV.Core.Domain;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.Channels
{
    internal static class Mapper
    {
        internal static ChannelViewModel ProjectToViewModel(Channel channel) =>
            new(
                channel.Id,
                channel.Number,
                channel.Name,
                channel.FFmpegProfileId,
                GetLogo(channel),
                channel.StreamingMode);

        private static string GetLogo(Channel channel) =>
            Optional(channel.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.Logo))
                .Match(a => a.Path, string.Empty);
    }
}
