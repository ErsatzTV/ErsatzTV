using System.Net;
using ErsatzTV.Application.Artworks;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Channels;

public record ChannelViewModel(
    int Id,
    string Number,
    string Name,
    string Group,
    string Categories,
    int FFmpegProfileId,
    ArtworkContentTypeModel Logo,
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
    ChannelSongVideoMode SongVideoMode,
    ChannelActiveMode ActiveMode)
{
    public string WebEncodedName => WebUtility.UrlEncode(Name);
}
