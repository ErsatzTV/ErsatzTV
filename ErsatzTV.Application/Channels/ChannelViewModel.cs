using ErsatzTV.Core.Domain;
using System.Net;

namespace ErsatzTV.Application.Channels;

public record ChannelViewModel(
    int Id,
    string Number,
    string Name,
    string Group,
    string Categories,
    int FFmpegProfileId,
    string Logo,
    ChannelStreamSelectorMode StreamSelectorMode,
    string StreamSelector,
    string PreferredAudioLanguageCode,
    string PreferredAudioTitle,
    ChannelProgressMode ProgressMode,
    StreamingMode StreamingMode,
    int? WatermarkId,
    int? FallbackFillerId,
    int PlayoutCount,
    string PreferredSubtitleLanguageCode,
    ChannelSubtitleMode SubtitleMode,
    ChannelMusicVideoCreditsMode MusicVideoCreditsMode,
    string MusicVideoCreditsTemplate,
    ChannelSongVideoMode SongVideoMode)
{
    public string WebEncodedName => WebUtility.UrlEncode(Name);
}
