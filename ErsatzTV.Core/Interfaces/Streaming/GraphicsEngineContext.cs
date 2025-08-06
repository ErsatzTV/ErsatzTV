using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;

namespace ErsatzTV.Core.Interfaces.Streaming;

public record GraphicsEngineContext(
    List<GraphicsElementContext> Elements,
    Resolution FrameSize,
    int FrameRate,
    TimeSpan Duration);

public abstract record GraphicsElementContext;

public record WatermarkElementContext(WatermarkOptions Options) : GraphicsElementContext;
