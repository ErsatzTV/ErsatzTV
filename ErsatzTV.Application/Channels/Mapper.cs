using ErsatzTV.Core.Api.Channels;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Channels;

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
            channel.StreamSelectorMode,
            channel.StreamSelector,
            channel.PreferredAudioLanguageCode,
            channel.PreferredAudioTitle,
            channel.ProgressMode,
            channel.StreamingMode,
            channel.WatermarkId,
            channel.FallbackFillerId,
            channel.Playouts?.Count ?? 0,
            channel.PreferredSubtitleLanguageCode,
            channel.SubtitleMode,
            channel.MusicVideoCreditsMode,
            channel.MusicVideoCreditsTemplate,
            channel.SongVideoMode,
            channel.ActiveMode);

    internal static ChannelResponseModel ProjectToResponseModel(Channel channel) =>
        new(
            channel.Id,
            channel.Number,
            channel.Name,
            channel.FFmpegProfile.Name,
            channel.PreferredAudioLanguageCode,
            GetStreamingMode(channel));

    internal static ResolutionViewModel ProjectToViewModel(Resolution resolution) =>
        new(resolution.Height, resolution.Width);

    internal static ResolutionAndBitrateViewModel ProjectToViewModel(Resolution resolution, int bitrate) =>
        new(resolution.Height, resolution.Width, bitrate);

    private static string GetLogo(Channel channel)
    {
        Option<Artwork> maybeArtwork = channel.Artwork
            .Where(a => a.ArtworkKind == ArtworkKind.Logo)
            .HeadOrNone();

        foreach (Artwork artwork in maybeArtwork)
        {
            return artwork.IsExternalUrl() ? artwork.Path : $"iptv/logos/{artwork.Path}";
        }

        return string.Empty;
    }

    private static string GetStreamingMode(Channel channel) =>
        channel.StreamingMode switch
        {
            StreamingMode.TransportStream => "MPEG-TS (Legacy)",
            StreamingMode.TransportStreamHybrid => "MPEG-TS",
            StreamingMode.HttpLiveStreamingDirect => "HLS Direct",
            StreamingMode.HttpLiveStreamingSegmenter => "HLS Segmenter",
            StreamingMode.HttpLiveStreamingSegmenterV2 => "HLS Segmenter V2",
            _ => throw new ArgumentOutOfRangeException(nameof(channel))
        };
}
