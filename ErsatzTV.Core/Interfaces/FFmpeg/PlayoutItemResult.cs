using CliWrap;
using ErsatzTV.Core.Interfaces.Streaming;

namespace ErsatzTV.Core.Interfaces.FFmpeg;

public record PlayoutItemResult(
    Command Process,
    Option<GraphicsEngineContext> GraphicsEngineContext,
    Option<int> MediaItemId);

