using CliWrap;
using ErsatzTV.Core.Interfaces.Streaming;

namespace ErsatzTV.Application.Streaming;

public class PlayoutItemProcessModel
{
    public PlayoutItemProcessModel(
        Command process,
        Option<GraphicsEngineContext> graphicsEngineContext,
        Option<TimeSpan> maybeDuration,
        DateTimeOffset until,
        bool isComplete,
        Option<long> segmentKey,
        Option<int> mediaItemId,
        Option<TimeSpan> playoutOffset)
    {
        Process = process;
        GraphicsEngineContext = graphicsEngineContext;
        MaybeDuration = maybeDuration;
        Until = until;
        IsComplete = isComplete;
        SegmentKey = segmentKey;
        MediaItemId = mediaItemId;

        // undo the offset applied in FFmpegProcessHandler
        // so we don't continually walk backward/forward in time by the offset amount
        foreach (TimeSpan offset in playoutOffset)
        {
            foreach (long key in SegmentKey)
            {
                SegmentKey = key + (long)offset.TotalSeconds;
            }

            Until += offset;
        }
    }

    public Command Process { get; init; }

    public Option<GraphicsEngineContext> GraphicsEngineContext { get; init; }

    public Option<TimeSpan> MaybeDuration { get; init; }

    public DateTimeOffset Until { get; init; }

    public bool IsComplete { get; init; }

    public Option<long> SegmentKey { get; init; }

    public Option<int> MediaItemId { get; init; }
}
