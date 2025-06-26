using ErsatzTV.Application.Channels;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.ViewModels;

public class ChannelEditViewModel
{
    private string _musicVideoCreditsTemplate;
    public int Id { get; set; }
    public string Name { get; set; }
    public string Group { get; set; }
    public string Categories { get; set; }
    public string Number { get; set; }
    public int FFmpegProfileId { get; set; }
    public ChannelStreamSelectorMode StreamSelectorMode { get; set; }
    public string StreamSelector { get; set; }
    public string PreferredAudioLanguageCode { get; set; }
    public string PreferredAudioTitle { get; set; }
    public string Logo { get; set; }
    public string ExternalLogoUrl { get; set; }
    public ChannelProgressMode ProgressMode { get; set; }
    public StreamingMode StreamingMode { get; set; }
    public int? WatermarkId { get; set; }
    public int? FallbackFillerId { get; set; }
    public string PreferredSubtitleLanguageCode { get; set; }
    public ChannelSubtitleMode SubtitleMode { get; set; }
    public ChannelMusicVideoCreditsMode MusicVideoCreditsMode { get; set; }
    public string MusicVideoCreditsTemplate
    {
        get => MusicVideoCreditsMode == ChannelMusicVideoCreditsMode.GenerateSubtitles
            ? _musicVideoCreditsTemplate
            : null;
        set => _musicVideoCreditsTemplate = value;
    }
    public ChannelSongVideoMode SongVideoMode { get; set; }

    public UpdateChannel ToUpdate() =>
        new(
            Id,
            Name,
            Number,
            Group,
            Categories,
            FFmpegProfileId,
            string.IsNullOrWhiteSpace(ExternalLogoUrl) ? Logo : ExternalLogoUrl,
            StreamSelectorMode,
            StreamSelector,
            PreferredAudioLanguageCode,
            PreferredAudioTitle,
            ProgressMode,
            StreamingMode,
            WatermarkId,
            FallbackFillerId,
            PreferredSubtitleLanguageCode,
            SubtitleMode,
            MusicVideoCreditsMode,
            MusicVideoCreditsTemplate,
            SongVideoMode);

    public CreateChannel ToCreate() =>
        new(
            Name,
            Number,
            Group,
            Categories,
            FFmpegProfileId,
            string.IsNullOrWhiteSpace(ExternalLogoUrl) ? Logo : ExternalLogoUrl,
            StreamSelectorMode,
            StreamSelector,
            PreferredAudioLanguageCode,
            PreferredAudioTitle,
            ProgressMode,
            StreamingMode,
            WatermarkId,
            FallbackFillerId,
            PreferredSubtitleLanguageCode,
            SubtitleMode,
            MusicVideoCreditsMode,
            MusicVideoCreditsTemplate,
            SongVideoMode);
}
