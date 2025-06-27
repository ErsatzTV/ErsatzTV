using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.FFmpeg;

public record StreamSelectorResult(Option<MediaStream> AudioStream, Option<Subtitle> Subtitle)
{
    public static readonly StreamSelectorResult None = new(Option<MediaStream>.None, Option<Subtitle>.None);
}
