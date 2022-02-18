using ErsatzTV.FFmpeg.Format;
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