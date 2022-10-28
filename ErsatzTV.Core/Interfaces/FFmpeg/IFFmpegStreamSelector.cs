using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;

namespace ErsatzTV.Core.Interfaces.FFmpeg;

public interface IFFmpegStreamSelector
{
    Task<MediaStream> SelectVideoStream(MediaVersion version);

    Task<Option<MediaStream>> SelectAudioStream(
        MediaItemAudioVersion version,
        StreamingMode streamingMode,
        Channel channel,
        string preferredAudioLanguage,
        string preferredAudioTitle);

    Task<Option<Subtitle>> SelectSubtitleStream(
        List<Subtitle> subtitles,
        Channel channel,
        string preferredSubtitleLanguage,
        ChannelSubtitleMode subtitleMode);
}
