using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.FFmpeg;

public interface IFFmpegStreamSelector
{
    Task<MediaStream> SelectVideoStream(MediaVersion version);

    Task<Option<MediaStream>> SelectAudioStream(
        MediaVersion version,
        StreamingMode streamingMode,
        string channelNumber,
        string preferredAudioLanguage);

    Task<Option<Subtitle>> SelectSubtitleStream(
        MediaVersion version,
        List<Subtitle> subtitles,
        StreamingMode streamingMode,
        string channelNumber,
        string preferredSubtitleLanguage,
        ChannelSubtitleMode subtitleMode);
}
