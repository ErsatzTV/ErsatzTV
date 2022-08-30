using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.FFmpeg;

public interface IFFmpegStreamSelector
{
    Task<MediaStream> SelectVideoStream(MediaVersion version);

    Task<Option<MediaStream>> SelectAudioStream(
        MediaVersion version,
        StreamingMode streamingMode,
        string channelNumber,
        string preferredAudioLanguage,
        string preferredAudioTitle);

    Task<Option<Subtitle>> SelectSubtitleStream(
        List<Subtitle> subtitles,
        Channel channel,
        string preferredSubtitleLanguage,
        ChannelSubtitleMode subtitleMode);
}
