using ErsatzTV.Application.Artworks;
using ErsatzTV.Core.Api.Channels;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Channels;

internal static class Mapper
{
    internal static ChannelViewModel ProjectToViewModel(Channel channel, int playoutCount) =>
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
            channel.PlayoutSource,
            channel.PlayoutMode,
            channel.MirrorSourceChannelId,
            channel.PlayoutOffset,
            channel.StreamingMode,
            channel.WatermarkId,
            channel.FallbackFillerId,
            playoutCount,
            channel.PreferredSubtitleLanguageCode,
            channel.SubtitleMode,
            channel.MusicVideoCreditsMode,
            channel.MusicVideoCreditsTemplate,
            channel.SongVideoMode,
            channel.TranscodeMode,
            channel.IdleBehavior,
            channel.IsEnabled,
            channel.ShowInEpg);

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

    private static ArtworkContentTypeModel GetLogo(Channel channel)
    {
        Option<Artwork> maybeArtwork = channel.Artwork
            .Where(a => a.ArtworkKind == ArtworkKind.Logo)
            .HeadOrNone();

        foreach (Artwork artwork in maybeArtwork)
        {
            return artwork.IsExternalUrl()
                ? new ArtworkContentTypeModel(artwork.Path, string.Empty)
                : new ArtworkContentTypeModel($"iptv/logos/{artwork.Path}", artwork.OriginalContentType);
        }

        return ArtworkContentTypeModel.None;
    }

    private static string GetStreamingMode(Channel channel) =>
        channel.StreamingMode switch
        {
            StreamingMode.TransportStream => "MPEG-TS (Legacy)",
            StreamingMode.TransportStreamHybrid => "MPEG-TS",
            StreamingMode.HttpLiveStreamingDirect => "HLS Direct",
            StreamingMode.HttpLiveStreamingSegmenter => "HLS Segmenter",
            StreamingMode.HttpLiveStreamingSegmenterFmp4 => "HLS Segmenter (fmp4)",
            StreamingMode.HttpLiveStreamingSegmenterV2 => "HLS Segmenter V2",
            _ => throw new ArgumentOutOfRangeException(nameof(channel))
        };
}
