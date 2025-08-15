using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Graphics;

namespace ErsatzTV.Core.Interfaces.Streaming;

public record GraphicsEngineContext(
    string ChannelNumber,
    MediaItem MediaItem,
    List<GraphicsElementContext> Elements,
    Resolution SquarePixelFrameSize,
    Resolution FrameSize,
    int FrameRate,
    DateTimeOffset ChannelStartTime,
    DateTimeOffset ContentStartTime,
    TimeSpan Seek,
    TimeSpan Duration);

public abstract record GraphicsElementContext;

public record WatermarkElementContext(WatermarkOptions Options) : GraphicsElementContext;

public record TextElementDataContext(TextGraphicsElement TextElement, Dictionary<string, string> Variables)
    : GraphicsElementContext, ITemplateDataContext
{
    public int EpgEntries => TextElement.EpgEntries;
}

public record ImageElementContext(ImageGraphicsElement ImageElement) : GraphicsElementContext;

public record SubtitleElementDataContext(SubtitlesGraphicsElement SubtitlesElement, Dictionary<string, string> Variables)
    : GraphicsElementContext, ITemplateDataContext
{
    public int EpgEntries => SubtitlesElement.EpgEntries;
}

public interface ITemplateDataContext
{
    int EpgEntries { get; }
}
