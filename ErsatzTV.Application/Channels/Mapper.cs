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
                channel.PreferredLanguageCode,
                channel.StreamingMode,
                channel.Watermark?.Mode ?? ChannelWatermarkMode.None,
                channel.Watermark?.Location ?? ChannelWatermarkLocation.BottomRight,
                channel.Watermark?.Size ?? ChannelWatermarkSize.Scaled,
                channel.Watermark?.WidthPercent ?? 15,
                channel.Watermark?.HorizontalMarginPercent ?? 5,
                channel.Watermark?.VerticalMarginPercent ?? 5,
                channel.Watermark?.FrequencyMinutes ?? 15,
                channel.Watermark?.DurationSeconds ?? 15,
                channel.Watermark?.Opacity ?? 100);

        private static string GetLogo(Channel channel) =>
            Optional(channel.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.Logo))
                .Match(a => a.Path, string.Empty);
    }
}
