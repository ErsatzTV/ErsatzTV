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
                channel.Group,
                channel.Categories,
                channel.FFmpegProfileId,
                GetLogo(channel),
                channel.PreferredLanguageCode,
                channel.StreamingMode,
                channel.WatermarkId,
                channel.FallbackFillerId,
                channel.Playouts?.Count ?? 0);

        private static string GetLogo(Channel channel) =>
            Optional(channel.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.Logo))
                .Match(a => a.Path, string.Empty);

        private static string GetWatermark(Channel channel) =>
            Optional(channel.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.Watermark))
                .Match(a => a.Path, string.Empty);
    }
}
