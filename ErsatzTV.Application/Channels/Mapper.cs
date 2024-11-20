﻿using ErsatzTV.Core.Api.Channels;
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
            channel.SongVideoMode);

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

    private static string GetLogo(Channel channel) =>
        Optional(channel.Artwork.FirstOrDefault(a => a.ArtworkKind == ArtworkKind.Logo))
            .Match(a => a.Path, string.Empty);

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
