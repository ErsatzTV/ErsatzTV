using CliWrap;
using ErsatzTV.Core.Interfaces.Streaming;

namespace ErsatzTV.Application.Streaming;

public record PlayoutItemProcessModel(
    Command Process,
    Option<GraphicsEngineContext> GraphicsEngineContext,
    Option<TimeSpan> MaybeDuration,
    DateTimeOffset Until,
    bool IsComplete,
    Option<long> SegmentKey);
