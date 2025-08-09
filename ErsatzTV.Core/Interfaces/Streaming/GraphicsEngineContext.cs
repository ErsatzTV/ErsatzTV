using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Graphics;

namespace ErsatzTV.Core.Interfaces.Streaming;

public record GraphicsEngineContext(
    MediaItem MediaItem,
    List<GraphicsElementContext> Elements,
    Resolution FrameSize,
    int FrameRate,
    DateTimeOffset ChannelStartTime,
    DateTimeOffset ContentStartTime,
    TimeSpan Seek,
    TimeSpan Duration);

public abstract record GraphicsElementContext;

public record WatermarkElementContext(WatermarkOptions Options) : GraphicsElementContext;

public record TextElementContext(TextGraphicsElement TextElement, Dictionary<string, string> Variables)
    : GraphicsElementContext;
