﻿using ErsatzTV.Application.Channels;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.ViewModels;

public class ChannelEditViewModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Group { get; set; }
    public string Categories { get; set; }
    public string Number { get; set; }
    public int FFmpegProfileId { get; set; }
    public string PreferredAudioLanguageCode { get; set; }
    public string Logo { get; set; }
    public StreamingMode StreamingMode { get; set; }
    public int? WatermarkId { get; set; }
    public int? FallbackFillerId { get; set; }
    public string PreferredSubtitleLanguageCode { get; set; }
    public ChannelSubtitleMode SubtitleMode { get; set; }
    public ChannelMusicVideoCreditsMode MusicVideoCreditsMode { get; set; }

    public UpdateChannel ToUpdate() =>
        new(
            Id,
            Name,
            Number,
            Group,
            Categories,
            FFmpegProfileId,
            Logo,
            PreferredAudioLanguageCode,
            StreamingMode,
            WatermarkId,
            FallbackFillerId,
            PreferredSubtitleLanguageCode,
            SubtitleMode,
            MusicVideoCreditsMode);

    public CreateChannel ToCreate() =>
        new(
            Name,
            Number,
            Group,
            Categories,
            FFmpegProfileId,
            Logo,
            PreferredAudioLanguageCode,
            StreamingMode,
            WatermarkId,
            FallbackFillerId,
            PreferredSubtitleLanguageCode,
            SubtitleMode,
            MusicVideoCreditsMode);
}
