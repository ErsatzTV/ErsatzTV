using ErsatzTV.FFmpeg.Format;
using ErsatzTV.FFmpeg.State;
using LanguageExt;

namespace ErsatzTV.FFmpeg;

public record InputFile(string Path, IList<MediaStream> Streams);

public record ConcatInputFile(string Url, FrameSize Resolution) : InputFile(Url, new List<MediaStream>
{
    new VideoStream(
        0,
        string.Empty,
        Option<IPixelFormat>.None,
        Resolution,
        Option<string>.None,
        false)
});

public record AudioInputFile(string Path, IList<AudioStream> AudioStreams) : InputFile(
    Path,
    AudioStreams.Cast<MediaStream>().ToList())
{
    public AudioState DesiredState { get; set; }
}

public record VideoInputFile(string Path, IList<VideoStream> VideoStreams) : InputFile(
    Path,
    VideoStreams.Cast<MediaStream>().ToList())
{
    public VideoState DesiredState { get; set; }
}
